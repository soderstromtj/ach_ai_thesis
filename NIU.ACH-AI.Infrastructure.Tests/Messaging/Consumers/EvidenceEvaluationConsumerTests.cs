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

public class EvidenceEvaluationConsumerTests : IDisposable
{
    private readonly Mock<IWorkflowPersistence> _mockWorkflowPersistence;
    private readonly Mock<ILogger<EvidenceEvaluationConsumer>> _mockLogger;
    private readonly EvidenceEvaluationConsumer _consumer;
    private readonly Mock<ConsumeContext<IEvidenceEvaluationRequested>> _mockContext;
    private readonly Mock<ISendEndpoint> _mockSendEndpoint;

    public EvidenceEvaluationConsumerTests()
    {
        _mockWorkflowPersistence = new Mock<IWorkflowPersistence>();
        _mockLogger = new Mock<ILogger<EvidenceEvaluationConsumer>>();
        _mockContext = new Mock<ConsumeContext<IEvidenceEvaluationRequested>>();
        _mockSendEndpoint = new Mock<ISendEndpoint>();

        _consumer = new EvidenceEvaluationConsumer(
            _mockWorkflowPersistence.Object,
            _mockLogger.Object);

        // Setup Endpoint Convention for testing Send validation
        EndpointConvention.Map<IEvaluateHypothesisEvidencePair>(new Uri("queue:test-queue"));

        // Setup GetSendEndpoint to return our mock
        _mockContext.Setup(x => x.GetSendEndpoint(It.IsAny<Uri>()))
            .ReturnsAsync(_mockSendEndpoint.Object);
    }

    public void Dispose()
    {
        // Not easily possible to remove convention for specific type, but manageable for unit tests
        // real clean way depends on MT version, but for now we just map.
    }

    [Fact]
    public async Task Consume_ValidInput_CreatesStepAndDispatchesEvaluations()
    {
        // Arrange
        var experimentId = Guid.NewGuid();
        var evidence1 = new Evidence { EvidenceId = Guid.NewGuid(), Claim = "Evidence 1" };
        var evidence2 = new Evidence { EvidenceId = Guid.NewGuid(), Claim = "Evidence 2" };
        var hypothesis1 = new Hypothesis { HypothesisId = Guid.NewGuid(), HypothesisText = "Hypothesis 1" };
        var hypothesis2 = new Hypothesis { HypothesisId = Guid.NewGuid(), HypothesisText = "Hypothesis 2" };

        var command = new
        {
            ExperimentId = experimentId,
            Configuration = new ACHStepConfiguration(),
            Input = new OrchestrationPromptInput
            {
                EvidenceResult = new EvidenceResult { Evidence = new List<Evidence> { evidence1, evidence2 } },
                HypothesisResult = new HypothesisResult { Hypotheses = new List<Hypothesis> { hypothesis1, hypothesis2 } }
            },
            StepContext = new StepExecutionContext(),
            HypothesisStepExecutionId = Guid.NewGuid(),
            EvidenceStepExecutionId = Guid.NewGuid()
        };

        SetupContextGeneric(command);

        var persistedStepId = Guid.NewGuid();
        _mockWorkflowPersistence
            .Setup(x => x.CreateStepExecutionAsync(
                It.Is<Guid>(id => id == experimentId),
                It.IsAny<ACHStepConfiguration>(),
                It.IsAny<Guid?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new StepExecutionContext { StepExecutionId = persistedStepId });

        // Act
        await _consumer.Consume(_mockContext.Object);

        // Assert
        // 1. Verify CreateStepExecutionAsync
        _mockWorkflowPersistence.Verify(x => x.CreateStepExecutionAsync(
            experimentId,
            It.IsAny<ACHStepConfiguration>(),
            It.IsAny<Guid?>(),
            It.IsAny<CancellationToken>()), Times.Once);

        // 2. Verify IEvaluationBatchStarted published
        _mockContext.Verify(x => x.Publish<IEvaluationBatchStarted>(
            It.Is<object>(val => VerifyBatchStarted(val, experimentId, persistedStepId, 4)),
            It.IsAny<CancellationToken>()), Times.Once);

        // 3. Verify IEvaluateHypothesisEvidencePair sent 4 times (2 ev * 2 hyp)
        _mockSendEndpoint.Verify(x => x.Send(
            It.IsAny<IEvaluateHypothesisEvidencePair>(),
            It.IsAny<IPipe<SendContext<IEvaluateHypothesisEvidencePair>>>(),
            It.IsAny<CancellationToken>()), Times.Exactly(4));
    }

    [Fact]
    public async Task Consume_DuplicateInput_DeduplicatesAndDispatches()
    {
        // Arrange
        var experimentId = Guid.NewGuid();
        var id1 = Guid.NewGuid();
        var evidence1 = new Evidence { EvidenceId = id1, Claim = "Evidence 1" };
        var evidenceDuplicate = new Evidence { EvidenceId = id1, Claim = "Evidence 1" }; 
        
        var hid1 = Guid.NewGuid();
        var hypothesis1 = new Hypothesis { HypothesisId = hid1, HypothesisText = "Hypothesis 1" };
        var hypothesisDuplicate = new Hypothesis { HypothesisId = hid1, HypothesisText = "Hypothesis 1" };

        var command = new
        {
            ExperimentId = experimentId,
            Configuration = new ACHStepConfiguration(),
            Input = new OrchestrationPromptInput
            {
                EvidenceResult = new EvidenceResult { Evidence = new List<Evidence> { evidence1, evidenceDuplicate } },
                HypothesisResult = new HypothesisResult { Hypotheses = new List<Hypothesis> { hypothesis1, hypothesisDuplicate } }
            },
            StepContext = new StepExecutionContext(),
            HypothesisStepExecutionId = Guid.NewGuid(),
            EvidenceStepExecutionId = Guid.NewGuid()
        };

        SetupContextGeneric(command);

        _mockWorkflowPersistence
            .Setup(x => x.CreateStepExecutionAsync(It.IsAny<Guid>(), It.IsAny<ACHStepConfiguration>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new StepExecutionContext { StepExecutionId = Guid.NewGuid() });

        // Act
        await _consumer.Consume(_mockContext.Object);

        // Assert
        // Should settle on 1 Evidence and 1 Hypothesis -> 1 evaluation
        _mockContext.Verify(x => x.Publish<IEvaluationBatchStarted>(
            It.Is<object>(val => GetTotalEvaluations(val) == 1),
            It.IsAny<CancellationToken>()), Times.Once);

        _mockSendEndpoint.Verify(x => x.Send(
            It.IsAny<IEvaluateHypothesisEvidencePair>(),
            It.IsAny<IPipe<SendContext<IEvaluateHypothesisEvidencePair>>>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }
    
    [Fact]
    public async Task Consume_EmptyGuids_FiltersAndDispatches()
    {
        // Arrange
        var experimentId = Guid.NewGuid();
        var evidence1 = new Evidence { EvidenceId = Guid.NewGuid(), Claim = "Valid" };
        var evidenceInvalid = new Evidence { EvidenceId = Guid.Empty, Claim = "Invalid" };
        
        var hypothesis1 = new Hypothesis { HypothesisId = Guid.NewGuid(), HypothesisText = "Valid" };
        var hypothesisInvalid = new Hypothesis { HypothesisId = Guid.Empty, HypothesisText = "Invalid" };

        var command = new
        {
            ExperimentId = experimentId,
            Configuration = new ACHStepConfiguration(),
            Input = new OrchestrationPromptInput
            {
                EvidenceResult = new EvidenceResult { Evidence = new List<Evidence> { evidence1, evidenceInvalid } },
                HypothesisResult = new HypothesisResult { Hypotheses = new List<Hypothesis> { hypothesis1, hypothesisInvalid } }
            },
            StepContext = new StepExecutionContext(),
            HypothesisStepExecutionId = Guid.NewGuid(),
            EvidenceStepExecutionId = Guid.NewGuid()
        };

        SetupContextGeneric(command);

        _mockWorkflowPersistence
            .Setup(x => x.CreateStepExecutionAsync(It.IsAny<Guid>(), It.IsAny<ACHStepConfiguration>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new StepExecutionContext { StepExecutionId = Guid.NewGuid() });

        // Act
        await _consumer.Consume(_mockContext.Object);

        // Assert
        // Should filter out empty GUIDs -> 1 * 1 = 1 evaluation
        _mockSendEndpoint.Verify(x => x.Send(
            It.IsAny<IEvaluateHypothesisEvidencePair>(), 
            It.IsAny<IPipe<SendContext<IEvaluateHypothesisEvidencePair>>>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Consume_NullOrEmptyLists_DoesNotDispatchEvaluations()
    {
        // Arrange
        var command = new
        {
            ExperimentId = Guid.NewGuid(),
            Configuration = new ACHStepConfiguration(),
            Input = new OrchestrationPromptInput
            {
                EvidenceResult = null, // Null inputs
                HypothesisResult = new HypothesisResult{ Hypotheses = new List<Hypothesis>() } // Empty list
            },
            StepContext = new StepExecutionContext(),
            HypothesisStepExecutionId = Guid.NewGuid(),
            EvidenceStepExecutionId = Guid.NewGuid()
        };

        SetupContextGeneric(command);

        _mockWorkflowPersistence
            .Setup(x => x.CreateStepExecutionAsync(It.IsAny<Guid>(), It.IsAny<ACHStepConfiguration>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new StepExecutionContext { StepExecutionId = Guid.NewGuid() });

        // Act
        await _consumer.Consume(_mockContext.Object);

        // Assert
        _mockSendEndpoint.Verify(x => x.Send(
            It.IsAny<IEvaluateHypothesisEvidencePair>(),
            It.IsAny<IPipe<SendContext<IEvaluateHypothesisEvidencePair>>>(),
            It.IsAny<CancellationToken>()), Times.Never);
            
         _mockContext.Verify(x => x.Publish<IEvaluationBatchStarted>(
            It.Is<object>(val => GetTotalEvaluations(val) == 0),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Consume_PersistenceFailure_LogsAndRethrows()
    {
        // Arrange
        var command = new
        {
            ExperimentId = Guid.NewGuid(),
            Configuration = new ACHStepConfiguration(),
            Input = new OrchestrationPromptInput(),
            StepContext = new StepExecutionContext()
        };

        SetupContextGeneric(command);

        var expectedException = new InvalidOperationException("DB Error");
        _mockWorkflowPersistence
            .Setup(x => x.CreateStepExecutionAsync(It.IsAny<Guid>(), It.IsAny<ACHStepConfiguration>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(expectedException);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => _consumer.Consume(_mockContext.Object));

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => true),
                expectedException,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    // Helpers
    private void SetupContextGeneric(dynamic commandObj)
    {
        var messageMock = new Mock<IEvidenceEvaluationRequested>();
        
        messageMock.Setup(m => m.ExperimentId).Returns((Guid)commandObj.ExperimentId);
        messageMock.Setup(m => m.Configuration).Returns((ACHStepConfiguration)commandObj.Configuration);
        messageMock.Setup(m => m.StepContext).Returns((StepExecutionContext)commandObj.StepContext);
        messageMock.Setup(m => m.Input).Returns((OrchestrationPromptInput)commandObj.Input);

        var type = (Type)commandObj.GetType();
        if (type.GetProperty("HypothesisStepExecutionId") != null)
            messageMock.Setup(m => m.HypothesisStepExecutionId).Returns((Guid)commandObj.HypothesisStepExecutionId);
            
        if (type.GetProperty("EvidenceStepExecutionId") != null)
            messageMock.Setup(m => m.EvidenceStepExecutionId).Returns((Guid)commandObj.EvidenceStepExecutionId);


        _mockContext.Setup(c => c.Message).Returns(messageMock.Object);
        _mockContext.Setup(c => c.CancellationToken).Returns(CancellationToken.None);
    }

    private bool VerifyBatchStarted(object actual, Guid expId, Guid stepId, int total)
    {
        if (actual == null) return false;
        var pExp = actual.GetType().GetProperty("ExperimentId")?.GetValue(actual);
        var pStep = actual.GetType().GetProperty("StepExecutionId")?.GetValue(actual);
        var pTotal = actual.GetType().GetProperty("TotalEvaluations")?.GetValue(actual);

        return (Guid)pExp! == expId && (Guid)pStep! == stepId && (int)pTotal! == total;
    }
    
    private int GetTotalEvaluations(object actual)
    {
         if (actual == null) return -1;
         var pTotal = actual.GetType().GetProperty("TotalEvaluations")?.GetValue(actual);
         return pTotal != null ? (int)pTotal : -1;
    }
}
