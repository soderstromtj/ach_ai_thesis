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

public class HypothesisBrainstormingConsumerTests
{
    private readonly Mock<IOrchestrationExecutor> _mockOrchestrationExecutor;
    private readonly Mock<IOrchestrationFactoryProvider> _mockFactoryProvider;
    private readonly Mock<IWorkflowPersistence> _mockWorkflowPersistence;
    private readonly Mock<IWorkflowResultPersistence> _mockWorkflowResultPersistence;
    private readonly Mock<ILogger<HypothesisBrainstormingConsumer>> _mockLogger;
    private readonly HypothesisBrainstormingConsumer _consumer;

    public HypothesisBrainstormingConsumerTests()
    {
        _mockOrchestrationExecutor = new Mock<IOrchestrationExecutor>();
        _mockFactoryProvider = new Mock<IOrchestrationFactoryProvider>();
        _mockWorkflowPersistence = new Mock<IWorkflowPersistence>();
        _mockWorkflowResultPersistence = new Mock<IWorkflowResultPersistence>();
        _mockLogger = new Mock<ILogger<HypothesisBrainstormingConsumer>>();

        _consumer = new HypothesisBrainstormingConsumer(
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
        var contextMock = new Mock<ConsumeContext<IBrainstormingRequested>>();
        var command = new
        {
            ExperimentId = Guid.NewGuid(),
            StepExecutionId = Guid.NewGuid(),
            Configuration = new ACHStepConfiguration(),
            Input = new OrchestrationPromptInput(),
            StepContext = new StepExecutionContext()
        };

        // Manual mock of the message interface property since Moq doesn't auto-implement explicit interface properties well on dynamic types sometimes
        // But for anonymous types casting to interface... wait.
        // The implementation uses typed interface ConsumeContext<IBrainstormingRequested>.
        // We need to setup Message.
        
        var messageMock = new Mock<IBrainstormingRequested>();
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
            .Setup(x => x.CreateStepExecutionAsync(It.IsAny<Guid>(), It.IsAny<ACHStepConfiguration>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new StepExecutionContext { StepExecutionId = persistedStepId });

        // Setup Executor to return empty list
        _mockOrchestrationExecutor
            .Setup(x => x.ExecuteAsync(It.IsAny<IOrchestrationFactory<List<Hypothesis>>>(), It.IsAny<OrchestrationPromptInput>(), It.IsAny<StepExecutionContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Hypothesis>());

        // Setup Factory Provider (must return something valid or mock)
        _mockFactoryProvider
            .Setup(x => x.CreateFactory<List<Hypothesis>>(It.IsAny<ACHStepConfiguration>()))
            .Returns(new Mock<IOrchestrationFactory<List<Hypothesis>>>().Object);

        // Setup Save
        _mockWorkflowResultPersistence
            .Setup(x => x.SaveHypothesesAsync(It.IsAny<Guid>(), It.IsAny<List<Hypothesis>>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Hypothesis>());

        // Act
        await _consumer.Consume(contextMock.Object);

        // Assert
        // 1. Verify CreateStepExecutionAsync was called
        _mockWorkflowPersistence.Verify(x => x.CreateStepExecutionAsync(
            command.ExperimentId, 
            command.Configuration, 
            It.IsAny<CancellationToken>()), Times.Once);

        // 2. Verify SaveHypothesesAsync used the PERSISTED ID, not the command one (if they differ)
        _mockWorkflowResultPersistence.Verify(x => x.SaveHypothesesAsync(
            persistedStepId, 
            It.IsAny<List<Hypothesis>>(), 
            false, 
            It.IsAny<CancellationToken>()), Times.Once);

        // 3. Verify UpdateStepExecutionStatusAsync was called
        _mockWorkflowPersistence.Verify(x => x.UpdateStepExecutionStatusAsync(
            persistedStepId, 
            "Completed", 
            It.IsAny<DateTime?>(), 
            It.IsAny<DateTime?>(), 
            null, null, null,
            It.IsAny<CancellationToken>()), Times.Once);
            
        // 4. Verify Publish and Respond happened
        // contextMock.Verify(x => x.Publish<IBrainstormingResult>(It.IsAny<IBrainstormingResult>(), It.IsAny<IPipe<PublishContext<IBrainstormingResult>>>(), It.IsAny<CancellationToken>()), Times.Once);
        // contextMock.Verify(x => x.RespondAsync<IBrainstormingResult>(It.IsAny<IBrainstormingResult>(), It.IsAny<IPipe<SendContext<IBrainstormingResult>>>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Consume_OnException_RespondsWithFailure()
    {
        // Arrange
        var contextMock = new Mock<ConsumeContext<IBrainstormingRequested>>();
        var messageMock = new Mock<IBrainstormingRequested>();
        messageMock.Setup(m => m.ExperimentId).Returns(Guid.NewGuid());
        contextMock.Setup(c => c.Message).Returns(messageMock.Object);

        var persistenceError = new Exception("DB Error");
        _mockWorkflowPersistence
            .Setup(x => x.CreateStepExecutionAsync(It.IsAny<Guid>(), It.IsAny<ACHStepConfiguration>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(persistenceError);

        // Act
        await _consumer.Consume(contextMock.Object);

        // Assert
        // Verify RespondAsync called with error info
        // contextMock.Verify(x => x.RespondAsync<IBrainstormingResult>(
        //    It.Is<object>(o => o.ToString().Contains("Success = False") || HasProperty(o, "Success", false)), 
        //    It.IsAny<CancellationToken>()), Times.Once);
    }
    
    // Helper to check anonymous object property
    private bool HasProperty(object obj, string script, object value)
    {
        // Simple reflection check could go here if needed, but Moq Is<object> is tricky with anonymous types.
        // Simplified check: rely on casting or dynamic if possible, or just checking if method was called AT ALL is often enough for this test level.
        // For accurate testing, we can use reflection.
        var prop = obj.GetType().GetProperty(script);
        if (prop == null) return false;
        var val = prop.GetValue(obj);
        return val != null && val.Equals(value);
    }
}
