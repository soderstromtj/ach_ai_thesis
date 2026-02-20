using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using NIU.ACH_AI.Application.Configuration;
using NIU.ACH_AI.Application.Interfaces;
using NIU.ACH_AI.Application.Services;

namespace NIU.ACH_AI.Application.Tests.Services;

public class ExperimentInitializationServiceTests
{
    private readonly Mock<IWorkflowPersistence> _mockPersistence;
    private readonly Mock<ILoggerFactory> _mockLoggerFactory;
    private readonly Mock<ILogger<ExperimentInitializationService>> _mockLogger;
    private readonly ExperimentInitializationService _service;

    public ExperimentInitializationServiceTests()
    {
        _mockPersistence = new Mock<IWorkflowPersistence>();
        _mockLoggerFactory = new Mock<ILoggerFactory>();
        _mockLogger = new Mock<ILogger<ExperimentInitializationService>>();

        _mockLoggerFactory
            .Setup(x => x.CreateLogger(It.IsAny<string>()))
            .Returns(_mockLogger.Object);

        _service = new ExperimentInitializationService(_mockPersistence.Object, _mockLoggerFactory.Object);
    }

    [Fact]
    public async Task InitializeExperimentAsync_CreatesScenarioAndExperiment()
    {
        // Arrange
        var config = new ExperimentConfiguration { Name = "TestExp", Context = "TestCtx" };
        var expectedScenarioId = Guid.NewGuid();
        var expectedExperimentId = Guid.NewGuid();

        _mockPersistence.Setup(x => x.CreateScenarioAsync(config.Context, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedScenarioId);

        _mockPersistence.Setup(x => x.CreateExperimentAsync(config, expectedScenarioId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedExperimentId);

        // Act
        var resultId = await _service.InitializeExperimentAsync(config);

        // Assert
        resultId.Should().Be(expectedExperimentId);
        _mockPersistence.Verify(x => x.CreateScenarioAsync(config.Context, It.IsAny<CancellationToken>()), Times.Once);
        _mockPersistence.Verify(x => x.CreateExperimentAsync(config, expectedScenarioId, It.IsAny<CancellationToken>()), Times.Once);
    }
}
