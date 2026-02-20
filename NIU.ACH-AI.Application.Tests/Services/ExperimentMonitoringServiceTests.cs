using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using NIU.ACH_AI.Application.DTOs;
using NIU.ACH_AI.Application.Interfaces;
using NIU.ACH_AI.Application.Services;

namespace NIU.ACH_AI.Application.Tests.Services;

public class ExperimentMonitoringServiceTests
{
    private readonly Mock<IWorkflowPersistence> _mockPersistence;
    private readonly Mock<ILoggerFactory> _mockLoggerFactory;
    private readonly Mock<ILogger<ExperimentMonitoringService>> _mockLogger;
    private readonly ExperimentMonitoringService _service;

    public ExperimentMonitoringServiceTests()
    {
        _mockPersistence = new Mock<IWorkflowPersistence>();
        _mockLoggerFactory = new Mock<ILoggerFactory>();
        _mockLogger = new Mock<ILogger<ExperimentMonitoringService>>();

        _mockLoggerFactory
            .Setup(x => x.CreateLogger(It.IsAny<string>()))
            .Returns(_mockLogger.Object);

        _service = new ExperimentMonitoringService(_mockPersistence.Object, _mockLoggerFactory.Object);
    }

    [Fact]
    public async Task WaitForCompletionAsync_ReturnsResult_WhenSagaCompletes()
    {
        // Arrange
        var expId = Guid.NewGuid();
        var resultToReturn = new ACHWorkflowResult { Success = true, ExperimentId = expId.ToString() };

        _mockPersistence.SetupSequence(x => x.GetSagaResultAsync(expId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ACHWorkflowResult?)null)
            .ReturnsAsync(resultToReturn);

        // Act
        var result = await _service.WaitForCompletionAsync(expId, "TestExp");

        // Assert
        result.Should().BeEquivalentTo(resultToReturn);
        _mockPersistence.Verify(x => x.GetSagaResultAsync(expId, It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task WaitForCompletionAsync_ReturnsCancelledResult_OnCancellation()
    {
        // Arrange
        var expId = Guid.NewGuid();
        var cts = new CancellationTokenSource();
        
        _mockPersistence.Setup(x => x.GetSagaResultAsync(expId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ACHWorkflowResult?)null)
            .Callback(() => cts.Cancel()); // Cancel after first poll

        // Act
        var result = await _service.WaitForCompletionAsync(expId, "TestExp", cts.Token);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Be("Detailed execution cancelled.");
    }
}
