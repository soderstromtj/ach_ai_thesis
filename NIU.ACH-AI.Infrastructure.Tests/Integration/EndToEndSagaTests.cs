using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using FluentAssertions;
using MassTransit;
using MassTransit.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using NIU.ACH_AI.Application.Configuration;
using NIU.ACH_AI.Application.DTOs;
using NIU.ACH_AI.Application.Interfaces;
using NIU.ACH_AI.Application.Messaging.Events;
using NIU.ACH_AI.Application.Services;
using NIU.ACH_AI.Domain.Entities;
using NIU.ACH_AI.Infrastructure.Messaging.Consumers;
using NIU.ACH_AI.Infrastructure.Persistence.Services;
using NIU.ACH_AI.Infrastructure.StateMachines;
using DbModels = NIU.ACH_AI.Infrastructure.Persistence.Models;

namespace NIU.ACH_AI.Infrastructure.Tests.Integration
{
    public class EndToEndSagaTests
    {
        [Fact]
        public async Task Should_Run_Full_Workflow_Successfully()
        {
            // Arrange
            var services = new ServiceCollection();

            // 1. Logging
            services.AddLogging(builder => builder.AddConsole());

            // 2. DbContexts (InMemory)
            services.AddDbContext<DbModels.AchAIDbContext>(options =>
                options.UseInMemoryDatabase("AchAiTestDb_" + Guid.NewGuid()));

            services.AddDbContext<DbModels.ACHSagaDbContext>(options =>
                options.UseInMemoryDatabase("AchSagaTestDb_" + Guid.NewGuid()));

            // 3. Persistence Services
            services.AddScoped<IWorkflowPersistence, WorkflowPersistence>();
            // Add other persistence interfaces if needed by consumers?
            // AgentConfigurationPersistence, AgentResponsePersistence...
            // Consumers use StepExecutionContext, which doesn't strictly depend on these unless persisting.
            // ACHWorkflowCoordinator uses AgentConfigurationPersistence.
            services.AddScoped<IAgentConfigurationPersistence>(sp => new Mock<IAgentConfigurationPersistence>().Object);
            services.AddScoped<IWorkflowResultPersistence>(sp => new Mock<IWorkflowResultPersistence>().Object);
            services.AddScoped<IOrchestrationFactoryProvider>(sp => new Mock<IOrchestrationFactoryProvider>().Object);

            // 4. Mock Executor
            var executorMock = new Mock<IOrchestrationExecutor>();

            // Setup for Brainstorming/Refinement (List<Hypothesis>)
            executorMock.Setup(x => x.ExecuteAsync(
                It.IsAny<IOrchestrationFactory<List<Hypothesis>>>(),
                It.IsAny<OrchestrationPromptInput>(),
                It.IsAny<StepExecutionContext>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync((IOrchestrationFactory<List<Hypothesis>> f, OrchestrationPromptInput i, StepExecutionContext ctx, CancellationToken t) =>
                {
                   var name = ctx?.AchStepName?.ToLowerInvariant() ?? "";
                   if (name.Contains("refinement") || name.Contains("evaluation")) // Hyp Evaluation usually creates separate result? 
                   // Wait, Refinement returns List<Hypothesis>.
                   // Brainstorming returns List<Hypothesis>.
                   {
                       return new List<Hypothesis> { new Hypothesis { HypothesisText = "H1-Refined" } };
                   }
                   return new List<Hypothesis> { new Hypothesis { HypothesisText = "H1" } };
                });

            // Setup for Evidence Extraction (List<Evidence>)
            executorMock.Setup(x => x.ExecuteAsync(
                It.IsAny<IOrchestrationFactory<List<Evidence>>>(),
                It.IsAny<OrchestrationPromptInput>(),
                It.IsAny<StepExecutionContext>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<Evidence> { new Evidence { Claim = "E1" } });

            // Setup for Evidence Evaluation (List<EvidenceHypothesisEvaluation>)
            executorMock.Setup(x => x.ExecuteAsync(
                It.IsAny<IOrchestrationFactory<List<EvidenceHypothesisEvaluation>>>(),
                It.IsAny<OrchestrationPromptInput>(),
                It.IsAny<StepExecutionContext>(),
                It.IsAny<CancellationToken>()))
                 .ReturnsAsync(new List<EvidenceHypothesisEvaluation> 
                 { 
                     new EvidenceHypothesisEvaluation 
                     { 
                         Score = NIU.ACH_AI.Domain.ValueObjects.EvaluationScore.Consistent, 
                         ScoreRationale = "Matches" 
                     } 
                 });

            services.AddScoped(_ => executorMock.Object);

            // 5. MassTransit with Saga and Consumers
            services.AddMassTransitTestHarness(cfg =>
            {
                // Register Consumers
                cfg.AddConsumer<HypothesisBrainstormingConsumer>();
                cfg.AddConsumer<HypothesisRefinementConsumer>(); // Handles Refinement
                cfg.AddConsumer<EvidenceExtractionConsumer>();
                cfg.AddConsumer<EvidenceEvaluationConsumer>();

                // Register Saga with EntityFramework Repository
                cfg.AddSagaStateMachine<ACHWorkflowStateMachine, DbModels.ExperimentState>()
                    .EntityFrameworkRepository(r =>
                    {
                        r.ExistingDbContext<DbModels.ACHSagaDbContext>();
                        // r.UseInMemoryDatabase(); // Not needed if DbContext is already InMemory? 
                        // Actually, MassTransit EF Repo needs explicit config sometimes.
                        // But ExistingDbContext is usually enough if DI handles it.
                    });
            });

            // 6. Coordinator
            services.AddScoped<IACHWorkflowCoordinator, ACHWorkflowCoordinator>();
            
            // 7. Request Clients (Still needed? Coordinator no longer uses them)
            // But MessageBrokerExtensions might have them. Here we manually register.
            // Coordinator constructor doesn't need them anymore.

            var provider = services.BuildServiceProvider(true);

            // Start Harness
            var harness = provider.GetRequiredService<ITestHarness>();
            await harness.Start();

            // Coordinator
            var coordinator = provider.GetRequiredService<IACHWorkflowCoordinator>();

            var config = new ExperimentConfiguration
            {
                Name = "E2E Test",
                Context = "Test Context",
                KeyQuestion = "Why?",
                ACHSteps = new[]
                {
                    new ACHStepConfiguration { Name = "HypothesisBrainstorming" },
                    new ACHStepConfiguration { Name = "EvidenceExtraction" }
                    // Keep it short for test
                }
            };

            // Act
            // ExecuteWorkflowAsync waits for completion
            var workflowTask = coordinator.ExecuteWorkflowAsync(config, CancellationToken.None);

            // Assert
            // Wait for task
            // We might need a timeout safer than infinite
            var success = await Task.WhenAny(workflowTask, Task.Delay(10000)) == workflowTask;
            
            if (!success)
            {
                // Timeout
                throw new Xunit.Sdk.XunitException("Workflow timed out.");
            }

            var result = await workflowTask;

            result.Success.Should().BeTrue();
            result.ExperimentId.Should().NotBeNullOrEmpty();
            result.Hypotheses.Should().NotBeNullOrEmpty();
            result.Evidence.Should().NotBeNullOrEmpty();
            
            // Verify Persisted Saga State independently
            var sagaDbContext = provider.GetRequiredService<DbModels.ACHSagaDbContext>();
            var state = await sagaDbContext.Set<DbModels.ExperimentState>().FirstOrDefaultAsync(x => x.CorrelationId.ToString() == result.ExperimentId);
            state.Should().NotBeNull();
            state.CurrentState.Should().Be("Completed");
        }
    }
}
