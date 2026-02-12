using MassTransit;
using Microsoft.Extensions.Logging;
using Moq;
using NIU.ACH_AI.Application.Configuration;
using NIU.ACH_AI.Application.DTOs;
using NIU.ACH_AI.Application.Interfaces;
using NIU.ACH_AI.Application.Messaging.Commands;
using NIU.ACH_AI.Application.Messaging.Events;
using DomainEntities = NIU.ACH_AI.Domain.Entities;
using NIU.ACH_AI.Infrastructure.Persistence.Models;
using NIU.ACH_AI.Infrastructure.Messaging.Consumers;
using NIU.ACH_AI.Domain.ValueObjects;

namespace NIU.ACH_AI.Infrastructure.Tests.Messaging.Consumers;

public class EvidenceEvaluationConsumerTests
{
    private readonly Mock<IOrchestrationExecutor> _orchestrationExecutorMock;
    private readonly Mock<IOrchestrationFactoryProvider> _factoryProviderMock;
    private readonly Mock<IWorkflowPersistence> _workflowPersistenceMock;
    private readonly Mock<IWorkflowResultPersistence> _workflowResultPersistenceMock;
    private readonly Mock<ILogger<EvidenceEvaluationConsumer>> _loggerMock;
    private readonly Mock<ConsumeContext<IEvidenceEvaluationRequested>> _contextMock;

    public EvidenceEvaluationConsumerTests()
    {
        _orchestrationExecutorMock = new Mock<IOrchestrationExecutor>();
        _factoryProviderMock = new Mock<IOrchestrationFactoryProvider>();
        _workflowPersistenceMock = new Mock<IWorkflowPersistence>();
        _workflowResultPersistenceMock = new Mock<IWorkflowResultPersistence>();
        _loggerMock = new Mock<ILogger<EvidenceEvaluationConsumer>>();
        _contextMock = new Mock<ConsumeContext<IEvidenceEvaluationRequested>>();
    }

    private EvidenceEvaluationConsumer CreateConsumer()
    {
        return new EvidenceEvaluationConsumer(
            _orchestrationExecutorMock.Object,
            _factoryProviderMock.Object,
            _workflowPersistenceMock.Object,
            _workflowResultPersistenceMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task Consume_SuccessfulProcessing_PublishesResult()
    {
        // Arrange
        var consumer = CreateConsumer();
        var command = CreateValidCommand(evidenceCount: 1, hypothesisCount: 1);
        _contextMock.Setup(x => x.Message).Returns(command);
        _contextMock.Setup(x => x.CancellationToken).Returns(CancellationToken.None);

        MockWorkflowPersistence(command);
        MockFactoryProvider();
        
        var expectedEvaluation = new DomainEntities.EvidenceHypothesisEvaluation { Score = NIU.ACH_AI.Domain.ValueObjects.EvaluationScore.Consistent, ScoreRationale = "Test" };
        MockOrchestrationExecutor(expectedEvaluation);

        // Act
        await consumer.Consume(_contextMock.Object);

        // Assert
        // 1. Verify Factory Creation
        _factoryProviderMock.Verify(x => x.CreateFactory<DomainEntities.EvidenceHypothesisEvaluation>(It.IsAny<ACHStepConfiguration>()), Times.Once);
        
        // 2. Verify Execution (1 evidence * 1 hypothesis = 1 execution)
        _orchestrationExecutorMock.Verify(x => x.ExecuteAsync(
            It.IsAny<IOrchestrationFactory<DomainEntities.EvidenceHypothesisEvaluation>>(),
            It.IsAny<OrchestrationPromptInput>(),
            It.IsAny<StepExecutionContext>(),
            It.IsAny<CancellationToken>()), Times.Once);

        // 3. Verify Persistence
        _workflowResultPersistenceMock.Verify(x => x.SaveEvaluationAsync(
            It.IsAny<Guid>(),
            It.IsAny<DomainEntities.EvidenceHypothesisEvaluation>(),
            It.IsAny<Guid>(),
            It.IsAny<Guid>(),
            It.IsAny<CancellationToken>()), Times.Once);

        // 4. Verify Publish
        _contextMock.Verify(x => x.Publish<IEvidenceEvaluationResult>(
            It.Is<object>(msg => VerifyResultMessage(msg, 1)),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Consume_MultipleItems_ExecutesNestedLoop()
    {
        // Arrange
        var consumer = CreateConsumer();
        // 2 Evidence * 3 Hypotheses = 6 Executions
        var command = CreateValidCommand(evidenceCount: 2, hypothesisCount: 3);
        _contextMock.Setup(x => x.Message).Returns(command);

        MockWorkflowPersistence(command);
        MockFactoryProvider();
        MockOrchestrationExecutor(new DomainEntities.EvidenceHypothesisEvaluation());

        // Act
        await consumer.Consume(_contextMock.Object);

        // Assert
        // Expect 6 executions
        _orchestrationExecutorMock.Verify(x => x.ExecuteAsync(
            It.IsAny<IOrchestrationFactory<DomainEntities.EvidenceHypothesisEvaluation>>(),
            It.IsAny<OrchestrationPromptInput>(),
            It.IsAny<StepExecutionContext>(),
            It.IsAny<CancellationToken>()), Times.Exactly(6));

        // Expect 6 saves
        _workflowResultPersistenceMock.Verify(x => x.SaveEvaluationAsync(
            It.IsAny<Guid>(),
            It.IsAny<DomainEntities.EvidenceHypothesisEvaluation>(),
            It.IsAny<Guid>(),
            It.IsAny<Guid>(),
            It.IsAny<CancellationToken>()), Times.Exactly(6));

        // Expect 1 publish event with 6 evaluations
         _contextMock.Verify(x => x.Publish<IEvidenceEvaluationResult>(
            It.Is<object>(msg => VerifyResultMessage(msg, 6)),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Consume_EmptyLists_DoesNotExecuteOrchestrator()
    {
        // Arrange
        var consumer = CreateConsumer();
        var command = CreateValidCommand(evidenceCount: 0, hypothesisCount: 0);
        _contextMock.Setup(x => x.Message).Returns(command);

        MockWorkflowPersistence(command);
        MockFactoryProvider();

        // Act
        await consumer.Consume(_contextMock.Object);

        // Assert
        _orchestrationExecutorMock.Verify(x => x.ExecuteAsync(
            It.IsAny<IOrchestrationFactory<DomainEntities.EvidenceHypothesisEvaluation>>(),
            It.IsAny<OrchestrationPromptInput>(),
            It.IsAny<StepExecutionContext>(),
            It.IsAny<CancellationToken>()), Times.Never);
            
         _contextMock.Verify(x => x.Publish<IEvidenceEvaluationResult>(
            It.Is<object>(msg => VerifyResultMessage(msg, 0)),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    #region Helpers

    private IEvidenceEvaluationRequested CreateValidCommand(int evidenceCount, int hypothesisCount)
    {
        var mock = new Mock<IEvidenceEvaluationRequested>();
        mock.Setup(m => m.ExperimentId).Returns(Guid.NewGuid());
        mock.Setup(m => m.StepContext).Returns(new StepExecutionContext { StepExecutionId = Guid.NewGuid() }); // Initial ID
        mock.Setup(m => m.Configuration).Returns(new ACHStepConfiguration { TaskInstructions = "Instructions" });
        
        var input = new OrchestrationPromptInput
        {
            KeyQuestion = "Q",
            Context = "C",
            EvidenceResult = new EvidenceResult { Evidence = Enumerable.Range(0, evidenceCount).Select(i => new DomainEntities.Evidence { Claim = $"E{i}" }).ToList() },
            HypothesisResult = new HypothesisResult { Hypotheses = Enumerable.Range(0, hypothesisCount).Select(i => new DomainEntities.Hypothesis { HypothesisText = $"H{i}" }).ToList() }
        };
        mock.Setup(m => m.Input).Returns(input);

        return mock.Object;
    }

    private void MockWorkflowPersistence(IEvidenceEvaluationRequested command)
    {
        // Return a NEW ID to verify the consumer uses the persisted ID, not the command ID
        var persistedId = Guid.NewGuid();
        _workflowPersistenceMock.Setup(x => x.CreateStepExecutionAsync(
            It.IsAny<Guid>(),
            It.IsAny<ACHStepConfiguration>(),
            It.IsAny<Guid?>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new StepExecutionContext { StepExecutionId = persistedId });
    }

    private void MockFactoryProvider()
    {
        _factoryProviderMock.Setup(x => x.CreateFactory<DomainEntities.EvidenceHypothesisEvaluation>(It.IsAny<ACHStepConfiguration>()))
            .Returns(new Mock<IOrchestrationFactory<DomainEntities.EvidenceHypothesisEvaluation>>().Object);
    }

    private void MockOrchestrationExecutor(DomainEntities.EvidenceHypothesisEvaluation result)
    {
        _orchestrationExecutorMock.Setup(x => x.ExecuteAsync(
            It.IsAny<IOrchestrationFactory<DomainEntities.EvidenceHypothesisEvaluation>>(),
            It.IsAny<OrchestrationPromptInput>(),
            It.IsAny<StepExecutionContext>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(result);
    }

    private bool VerifyResultMessage(object msg, int expectedCount)
    {
        // msg is an anonymous type. Reflection needed.
        var countProp = msg.GetType().GetProperty("Evaluations");
        if (countProp == null) return false;
        
        var list = countProp.GetValue(msg) as List<DomainEntities.EvidenceHypothesisEvaluation>;
        return list != null && list.Count == expectedCount;
    }

    #endregion
}
