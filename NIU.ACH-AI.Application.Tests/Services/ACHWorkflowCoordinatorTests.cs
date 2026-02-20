using FluentAssertions;
using MassTransit;
using Microsoft.Extensions.Logging;
using Moq;
using NIU.ACH_AI.Application.Configuration;
using NIU.ACH_AI.Application.DTOs;
using NIU.ACH_AI.Application.Interfaces;
using NIU.ACH_AI.Application.Messaging.Events;
using NIU.ACH_AI.Application.Services;

namespace NIU.ACH_AI.Application.Tests.Services;

/// <summary>
/// Unit tests for ACHWorkflowCoordinator which now delegates to Initialization and Monitoring services.
/// </summary>
public class ACHWorkflowCoordinatorTests
{
    private readonly Mock<IExperimentInitializationService> _mockInitService;
    private readonly Mock<IExperimentMonitoringService> _mockMonitorService;
    private readonly Mock<IPublishEndpoint> _mockPublishEndpoint;
    private readonly Mock<ILoggerFactory> _mockLoggerFactory;
    private readonly Mock<ILogger<ACHWorkflowCoordinator>> _mockLogger;
    private readonly ACHWorkflowCoordinator _coordinator;

    public ACHWorkflowCoordinatorTests()
    {
        _mockInitService = new Mock<IExperimentInitializationService>();
        _mockMonitorService = new Mock<IExperimentMonitoringService>();
        _mockPublishEndpoint = new Mock<IPublishEndpoint>();
        _mockLoggerFactory = new Mock<ILoggerFactory>();
        _mockLogger = new Mock<ILogger<ACHWorkflowCoordinator>>();

        _mockLoggerFactory
            .Setup(x => x.CreateLogger(It.IsAny<string>()))
            .Returns(_mockLogger.Object);

        _mockInitService
            .Setup(x => x.InitializeExperimentAsync(It.IsAny<ExperimentConfiguration>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Guid.NewGuid());

        _coordinator = new ACHWorkflowCoordinator(
            _mockInitService.Object,
            _mockMonitorService.Object,
            _mockPublishEndpoint.Object,
            _mockLoggerFactory.Object);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullInitService_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new ACHWorkflowCoordinator(
                null!,
                _mockMonitorService.Object,
                _mockPublishEndpoint.Object,
                _mockLoggerFactory.Object));
    }

    [Fact]
    public void Constructor_WithNullMonitorService_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new ACHWorkflowCoordinator(
                _mockInitService.Object,
                null!,
                _mockPublishEndpoint.Object,
                _mockLoggerFactory.Object));
    }

    [Fact]
    public void Constructor_WithNullPublishEndpoint_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new ACHWorkflowCoordinator(
                _mockInitService.Object,
                _mockMonitorService.Object,
                null!,
                _mockLoggerFactory.Object));
    }

    [Fact]
    public void Constructor_WithValidDependencies_CreatesInstance()
    {
        var coordinator = new ACHWorkflowCoordinator(
            _mockInitService.Object,
            _mockMonitorService.Object,
            _mockPublishEndpoint.Object,
            _mockLoggerFactory.Object);
        coordinator.Should().NotBeNull();
    }

    #endregion

    #region ExecuteWorkflowAsync Tests

    [Fact]
    public async Task ExecuteWorkflowAsync_CallsServicesAndReturnsSuccess()
    {
        // Arrange
        var config = new ExperimentConfiguration { Name = "Test", Context = "Ctx" };
        var experimentId = Guid.NewGuid();
        
        _mockInitService
            .Setup(x => x.InitializeExperimentAsync(It.IsAny<ExperimentConfiguration>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(experimentId);

        var successResult = new ACHWorkflowResult { Success = true, ExperimentId = experimentId.ToString() };
        _mockMonitorService
            .Setup(x => x.WaitForCompletionAsync(experimentId, config.Name, It.IsAny<CancellationToken>()))
            .ReturnsAsync(successResult);

        // Act
        var result = await _coordinator.ExecuteWorkflowAsync(config);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.ExperimentId.Should().Be(experimentId.ToString());

        // Verify Initialization happened
        _mockInitService.Verify(x => x.InitializeExperimentAsync(config, It.IsAny<CancellationToken>()), Times.Once);

        // Verify Publish happened
        _mockPublishEndpoint.Verify(x => x.Publish<IExperimentStarted>(
            It.IsAny<object>(), 
            It.IsAny<CancellationToken>()), Times.Once);

        // Verify Monitoring happened
        _mockMonitorService.Verify(x => x.WaitForCompletionAsync(experimentId, config.Name, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteWorkflowAsync_ReturnsFailure_WhenExceptionIsThrown()
    {
        // Arrange
        var config = new ExperimentConfiguration { Name = "Test", Context = "Ctx" };
        
        _mockInitService
            .Setup(x => x.InitializeExperimentAsync(It.IsAny<ExperimentConfiguration>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _coordinator.ExecuteWorkflowAsync(config);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Be("Database error");
    }

    #endregion
}
