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

public class HypothesisRefinementConsumerTests
{
    private readonly Mock<IOrchestrationExecutor> _mockOrchestrationExecutor;
    private readonly Mock<IOrchestrationFactoryProvider> _mockFactoryProvider;
    private readonly Mock<IWorkflowPersistence> _mockWorkflowPersistence;
    private readonly Mock<IWorkflowResultPersistence> _mockWorkflowResultPersistence;
    private readonly Mock<ILogger<HypothesisRefinementConsumer>> _mockLogger;
    private readonly HypothesisRefinementConsumer _consumer;

    public HypothesisRefinementConsumerTests()
    {
        _mockOrchestrationExecutor = new Mock<IOrchestrationExecutor>();
        _mockFactoryProvider = new Mock<IOrchestrationFactoryProvider>();
        _mockWorkflowPersistence = new Mock<IWorkflowPersistence>();
        _mockWorkflowResultPersistence = new Mock<IWorkflowResultPersistence>();
        _mockLogger = new Mock<ILogger<HypothesisRefinementConsumer>>();

        _consumer = new HypothesisRefinementConsumer(
            _mockOrchestrationExecutor.Object,
            _mockFactoryProvider.Object,
            _mockWorkflowPersistence.Object,
            _mockWorkflowResultPersistence.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task Consume_AttemptsToCreateStepExecution_BeforeProcessing()
    {
        // Arrange
        var contextMock = new Mock<ConsumeContext<IHypothesisRefinementRequested>>();
        var command = new
        {
            ExperimentId = Guid.NewGuid(),
            StepExecutionId = Guid.NewGuid(),
            Configuration = new ACHStepConfiguration(),
            Input = new OrchestrationPromptInput(),
            StepContext = new StepExecutionContext()
        };
        
        var messageMock = new Mock<IHypothesisRefinementRequested>();
        messageMock.Setup(m => m.ExperimentId).Returns(command.ExperimentId);
        messageMock.Setup(m => m.StepExecutionId).Returns(command.StepExecutionId);
        messageMock.Setup(m => m.Configuration).Returns(command.Configuration);
        messageMock.Setup(m => m.Input).Returns(command.Input);
        messageMock.Setup(m => m.StepContext).Returns(command.StepContext);

        contextMock.Setup(c => c.Message).Returns(messageMock.Object);
        contextMock.Setup(c => c.CancellationToken).Returns(CancellationToken.None);

        // Setup Persisted ID return
        var persistedStepId = Guid.NewGuid();
        _mockWorkflowPersistence
            .Setup(x => x.CreateStepExecutionAsync(It.IsAny<Guid>(), It.IsAny<ACHStepConfiguration>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new StepExecutionContext { StepExecutionId = persistedStepId });

        _mockOrchestrationExecutor
            .Setup(x => x.ExecuteAsync(It.IsAny<IOrchestrationFactory<List<Hypothesis>>>(), It.IsAny<OrchestrationPromptInput>(), It.IsAny<StepExecutionContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Hypothesis>());

        _mockFactoryProvider
            .Setup(x => x.CreateFactory<List<Hypothesis>>(It.IsAny<ACHStepConfiguration>()))
            .Returns(new Mock<IOrchestrationFactory<List<Hypothesis>>>().Object);

        _mockWorkflowResultPersistence
            .Setup(x => x.SaveHypothesesAsync(It.IsAny<Guid>(), It.IsAny<List<Hypothesis>>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Hypothesis>());

        // Act
        await _consumer.Consume(contextMock.Object);

        // Assert
        // 1. Verify CreateStepExecutionAsync
        _mockWorkflowPersistence.Verify(x => x.CreateStepExecutionAsync(
            command.ExperimentId, 
            command.Configuration, 
            It.IsAny<Guid?>(),
            It.IsAny<CancellationToken>()), Times.Once);

        // 2. Verify SaveHypothesesAsync used isRefined = TRUE
        _mockWorkflowResultPersistence.Verify(x => x.SaveHypothesesAsync(
            persistedStepId, 
            It.IsAny<List<Hypothesis>>(), 
            true, // isRefined must be true
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
        // contextMock.Verify(x => x.Publish<IHypothesisRefinementResult>(It.IsAny<IHypothesisRefinementResult>()), Times.Once);
        // contextMock.Verify(x => x.RespondAsync<IHypothesisRefinementResult>(It.IsAny<IHypothesisRefinementResult>()), Times.Once);
    }
}
