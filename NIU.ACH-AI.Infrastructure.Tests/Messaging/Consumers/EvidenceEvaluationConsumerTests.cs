using FluentAssertions;
using MassTransit;
using Microsoft.Extensions.Logging;
using Moq;
using NIU.ACH_AI.Application.Configuration;
using NIU.ACH_AI.Application.DTOs;
using NIU.ACH_AI.Application.Interfaces;
using NIU.ACH_AI.Application.Messaging.Commands;
using NIU.ACH_AI.Application.Messaging.Events;
using NIU.ACH_AI.Domain.Entities;
using NIU.ACH_AI.Infrastructure.Messaging.Consumers;
using Xunit;

namespace NIU.ACH_AI.Infrastructure.Tests.Messaging.Consumers;

public class EvidenceEvaluationConsumerTests
{
    private readonly Mock<IOrchestrationExecutor> _mockOrchestrationExecutor;
    private readonly Mock<IOrchestrationFactoryProvider> _mockFactoryProvider;
    private readonly Mock<IWorkflowPersistence> _mockWorkflowPersistence;
    private readonly Mock<IWorkflowResultPersistence> _mockWorkflowResultPersistence;
    private readonly Mock<ILogger<EvidenceEvaluationConsumer>> _mockLogger;
    private readonly EvidenceEvaluationConsumer _consumer;

    public EvidenceEvaluationConsumerTests()
    {
        _mockOrchestrationExecutor = new Mock<IOrchestrationExecutor>();
        _mockFactoryProvider = new Mock<IOrchestrationFactoryProvider>();
        _mockWorkflowPersistence = new Mock<IWorkflowPersistence>();
        _mockWorkflowResultPersistence = new Mock<IWorkflowResultPersistence>();
        _mockLogger = new Mock<ILogger<EvidenceEvaluationConsumer>>();

        _consumer = new EvidenceEvaluationConsumer(
            _mockOrchestrationExecutor.Object,
            _mockFactoryProvider.Object,
            _mockWorkflowPersistence.Object,
            _mockWorkflowResultPersistence.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task Consume_AttemptsToCreateStepExecution_BeforeProcessing_AndEvaluatesPairs()
    {
        // Arrange
        var contextMock = new Mock<ConsumeContext<IEvidenceEvaluationRequested>>();
        
        // Input with 1 Evidence and 1 Hypothesis -> 1 Evaluation
        var input = new OrchestrationPromptInput
        {
            EvidenceResult = new EvidenceResult 
            { 
                Evidence = new List<Evidence> { new Evidence { EvidenceId = Guid.NewGuid(), Claim = "E1" } } 
            },
            HypothesisResult = new HypothesisResult 
            { 
                Hypotheses = new List<Hypothesis> { new Hypothesis { HypothesisId = Guid.NewGuid(), ShortTitle = "H1" } } 
            }
        };

        var command = new
        {
            ExperimentId = Guid.NewGuid(),
            StepExecutionId = Guid.NewGuid(),
            Configuration = new ACHStepConfiguration(),
            Input = input,
            StepContext = new StepExecutionContext(),
            HypothesisStepExecutionId = Guid.NewGuid(),
            EvidenceStepExecutionId = Guid.NewGuid()
        };
        
        var messageMock = new Mock<IEvidenceEvaluationRequested>();
        messageMock.Setup(m => m.ExperimentId).Returns(command.ExperimentId);
        messageMock.Setup(m => m.StepExecutionId).Returns(command.StepExecutionId);
        messageMock.Setup(m => m.Configuration).Returns(command.Configuration);
        messageMock.Setup(m => m.Input).Returns(command.Input);
        messageMock.Setup(m => m.StepContext).Returns(command.StepContext);
        messageMock.Setup(m => m.HypothesisStepExecutionId).Returns(command.HypothesisStepExecutionId);
        messageMock.Setup(m => m.EvidenceStepExecutionId).Returns(command.EvidenceStepExecutionId);


        contextMock.Setup(c => c.Message).Returns(messageMock.Object);
        contextMock.Setup(c => c.CancellationToken).Returns(CancellationToken.None);

        // Setup Persisted ID return
        var persistedStepId = Guid.NewGuid();
        _mockWorkflowPersistence
            .Setup(x => x.CreateStepExecutionAsync(It.IsAny<Guid>(), It.IsAny<ACHStepConfiguration>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new StepExecutionContext { StepExecutionId = persistedStepId });

        _mockOrchestrationExecutor
            .Setup(x => x.ExecuteAsync(It.IsAny<IOrchestrationFactory<List<EvidenceHypothesisEvaluation>>>(), It.IsAny<OrchestrationPromptInput>(), It.IsAny<StepExecutionContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<EvidenceHypothesisEvaluation> { new EvidenceHypothesisEvaluation() });

        _mockFactoryProvider
            .Setup(x => x.CreateFactory<List<EvidenceHypothesisEvaluation>>(It.IsAny<ACHStepConfiguration>()))
            .Returns(new Mock<IOrchestrationFactory<List<EvidenceHypothesisEvaluation>>>().Object);

        _mockWorkflowResultPersistence
            .Setup(x => x.SaveEvaluationsAsync(It.IsAny<Guid>(), It.IsAny<List<EvidenceHypothesisEvaluation>>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _consumer.Consume(contextMock.Object);

        // Assert
        // 1. Verify CreateStepExecutionAsync
        _mockWorkflowPersistence.Verify(x => x.CreateStepExecutionAsync(
            command.ExperimentId, 
            command.Configuration, 
            It.IsAny<CancellationToken>()), Times.Once);

        // 2. Verify SaveEvaluationsAsync called (inside loop, so once for 1 pair)
        _mockWorkflowResultPersistence.Verify(x => x.SaveEvaluationsAsync(
            persistedStepId, 
            It.IsAny<List<EvidenceHypothesisEvaluation>>(), 
            command.HypothesisStepExecutionId,
            command.EvidenceStepExecutionId,
            It.IsAny<CancellationToken>()), Times.Once);

        // 3. Verify UpdateStepExecutionStatusAsync
        _mockWorkflowPersistence.Verify(x => x.UpdateStepExecutionStatusAsync(
            persistedStepId, 
            "Completed", 
            It.IsAny<DateTime?>(), 
            It.IsAny<DateTime?>(), 
            null, null, null,
            It.IsAny<CancellationToken>()), Times.Once);
            
        // 4. Verify Publish/Respond
        // contextMock.Verify(x => x.Publish<IEvidenceEvaluationResult>(It.IsAny<IEvidenceEvaluationResult>()), Times.Once);
        // contextMock.Verify(x => x.RespondAsync<IEvidenceEvaluationResult>(It.IsAny<IEvidenceEvaluationResult>()), Times.Once);
    }
}
