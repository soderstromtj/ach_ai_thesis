using System;
using System.Threading.Tasks;
using Xunit;
using FluentAssertions;
using MassTransit;
using MassTransit.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using NIU.ACH_AI.Infrastructure.StateMachines;
using NIU.ACH_AI.Infrastructure.StateMachines;
using DbModels = NIU.ACH_AI.Infrastructure.Persistence.Models;
using NIU.ACH_AI.Application.Messaging.Events;
using NIU.ACH_AI.Application.Messaging.Commands;
using NIU.ACH_AI.Application.Configuration;

namespace NIU.ACH_AI.Infrastructure.Tests.StateMachines
{
    public class ACHWorkflowStateMachineTests
    {
        [Fact]
        public async Task Should_Start_Saga_And_Dispatch_Brainstorming()
        {
            // Arrange
            var loggerMock = new Mock<ILogger<ACHWorkflowStateMachine>>();
            
            await using var provider = new ServiceCollection()
                .AddMassTransitTestHarness(cfg =>
                {
                    cfg.AddSagaStateMachine<ACHWorkflowStateMachine, DbModels.ExperimentState>()
                       .InMemoryRepository();

                    // Register dependencies for the machine if any (logger is injected via factory usually)
                    cfg.AddSingleton(loggerMock.Object);
                })
                .BuildServiceProvider(true);

            var harness = provider.GetRequiredService<ITestHarness>();

            await harness.Start();

            var experimentId = Guid.NewGuid();
            var config = new ExperimentConfiguration 
            { 
                Name = "Test Experiment",
                ACHSteps = new[]
                {
                    new ACHStepConfiguration { Name = "HypothesisBrainstorming" }
                }
            };

            // Act
            await harness.Bus.Publish<IExperimentStarted>(new 
            { 
                ExperimentId = experimentId,
                Configuration = config,
                Timestamp = DateTime.UtcNow
            });

            // Assert
            
            // 1. Saga should be created
            var sagaHarness = harness.GetSagaStateMachineHarness<ACHWorkflowStateMachine, DbModels.ExperimentState>();
            (await sagaHarness.Consumed.Any<IExperimentStarted>()).Should().BeTrue();
            (await sagaHarness.Created.Any(x => x.CorrelationId == experimentId)).Should().BeTrue();

            // 2. Should have transitioned to Brainstorming
            // Wait for generic consumption/processing
            // Since Dispatch happens in Initially, it should fire immediately.
            
            // Verify event published
            (await harness.Published.Any<IBrainstormingRequested>()).Should().BeTrue();

            // Verify state
            // Note: InMemory repository async might take a moment, but TestHarness methods usually handle waits or we use loops.
            // harness.Created.SelectAsync...
            
            var instance = sagaHarness.Created.Contains(experimentId);
            instance.Should().NotBeNull();
            
            // Check state using the instance in repository?
            // The harness exposes it via `sagaHarness.Sagas`
        }
    }
}
