using FluentAssertions;
using MassTransit;
using Microsoft.Extensions.Logging;
using Moq;
using NIU.ACH_AI.Application.Configuration;
using NIU.ACH_AI.Application.DTOs;
using NIU.ACH_AI.Application.Interfaces;
using NIU.ACH_AI.Application.Messaging.Events;
using NIU.ACH_AI.Application.Services;
using NIU.ACH_AI.Domain.Entities;

namespace NIU.ACH_AI.Application.Tests.Services;

/// <summary>
/// Unit tests for ACHWorkflowCoordinator which now uses Saga Orchestration.
/// </summary>
public class ACHWorkflowCoordinatorTests
{
    private readonly Mock<IPublishEndpoint> _mockPublishEndpoint;
    private readonly Mock<IWorkflowPersistence> _mockWorkflowPersistence;
    private readonly Mock<ILoggerFactory> _mockLoggerFactory;
    private readonly Mock<ILogger<ACHWorkflowCoordinator>> _mockLogger;
    private readonly ACHWorkflowCoordinator _coordinator;

    public ACHWorkflowCoordinatorTests()
    {
        _mockPublishEndpoint = new Mock<IPublishEndpoint>();
        _mockWorkflowPersistence = new Mock<IWorkflowPersistence>();
        _mockLoggerFactory = new Mock<ILoggerFactory>();
        _mockLogger = new Mock<ILogger<ACHWorkflowCoordinator>>();

        _mockLoggerFactory
            .Setup(x => x.CreateLogger(It.IsAny<string>()))
            .Returns(_mockLogger.Object);

        _mockWorkflowPersistence
            .Setup(x => x.CreateScenarioAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Guid.NewGuid());
        _mockWorkflowPersistence
            .Setup(x => x.CreateExperimentAsync(It.IsAny<ExperimentConfiguration>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Guid.NewGuid());

        _coordinator = new ACHWorkflowCoordinator(
            _mockPublishEndpoint.Object,
            _mockWorkflowPersistence.Object,
            _mockLoggerFactory.Object);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullPublishEndpoint_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new ACHWorkflowCoordinator(
                null!,
                _mockWorkflowPersistence.Object,
                _mockLoggerFactory.Object));
    }

    [Fact]
    public void Constructor_WithNullPersistence_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new ACHWorkflowCoordinator(
                _mockPublishEndpoint.Object,
                null!,
                _mockLoggerFactory.Object));
    }

    [Fact]
    public void Constructor_WithValidDependencies_CreatesInstance()
    {
        var coordinator = new ACHWorkflowCoordinator(
            _mockPublishEndpoint.Object,
            _mockWorkflowPersistence.Object,
            _mockLoggerFactory.Object);
        coordinator.Should().NotBeNull();
    }

    #endregion

    #region ExecuteWorkflowAsync Tests

    [Fact]
    public async Task ExecuteWorkflowAsync_StartsSagaAndPollsForCompletion()
    {
        // Arrange
        var config = new ExperimentConfiguration { Name = "Test", Context = "Ctx" };
        var experimentId = Guid.NewGuid();
        
        _mockWorkflowPersistence
            .Setup(x => x.CreateExperimentAsync(It.IsAny<ExperimentConfiguration>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(experimentId);

        // First poll returns null (running), second returns result (completed)
        _mockWorkflowPersistence
            .SetupSequence(x => x.GetSagaResultAsync(experimentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ACHWorkflowResult?)null)
            .ReturnsAsync(new ACHWorkflowResult { Success = true, ExperimentId = experimentId.ToString() });

        // Act
        var result = await _coordinator.ExecuteWorkflowAsync(config);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.ExperimentId.Should().Be(experimentId.ToString());

        // Verify Publish happened
        _mockPublishEndpoint.Verify(x => x.Publish(
            It.Is<IExperimentStarted>(e => e.Configuration == config), 
            It.IsAny<CancellationToken>()), Times.Once);

        // Verify Polling happened twice
        _mockWorkflowPersistence.Verify(x => x.GetSagaResultAsync(experimentId, It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task ExecuteWorkflowAsync_ReturnsFailure_WhenSagaFails()
    {
        // Arrange
        var config = new ExperimentConfiguration { Name = "Test", Context = "Ctx" };
        var experimentId = Guid.NewGuid();

        _mockWorkflowPersistence
            .Setup(x => x.CreateExperimentAsync(It.IsAny<ExperimentConfiguration>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(experimentId);

        _mockWorkflowPersistence
            .Setup(x => x.GetSagaResultAsync(experimentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ACHWorkflowResult { Success = false, ErrorMessage = "Saga Failed", ExperimentId = experimentId.ToString() });

        // Act
        var result = await _coordinator.ExecuteWorkflowAsync(config);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Be("Saga Failed");
    }

    #endregion
}
