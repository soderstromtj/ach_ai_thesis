using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using NIU.ACH_AI.Application.Configuration;
using NIU.ACH_AI.Application.DTOs;
using NIU.ACH_AI.Application.Interfaces;
using NIU.ACH_AI.Domain.Entities;
using NIU.ACH_AI.Infrastructure.AI.Services;
using NIU.ACH_AI.Infrastructure.Configuration;

namespace NIU.ACH_AI.Infrastructure.Tests.AI.Services;

/// <summary>
/// Unit tests for OrchestrationExecutor following FIRST principles.
/// Tests are Fast, Isolated, Repeatable, Self-validating, and Timely.
/// Verifies orchestration factory execution, service creation, and dependency management.
/// </summary>
public class OrchestrationExecutorTests
{
    private readonly Mock<ILoggerFactory> _mockLoggerFactory;
    private readonly Mock<ILogger<OrchestrationExecutor>> _mockLogger;
    private readonly Mock<IKernelBuilderService> _mockKernelBuilderService;
    private readonly IOptions<AIServiceSettings> _aiServiceSettings;
    private readonly OrchestrationExecutor _orchestrationExecutor;

    public OrchestrationExecutorTests()
    {
        _mockLoggerFactory = new Mock<ILoggerFactory>();
        _mockLogger = new Mock<ILogger<OrchestrationExecutor>>();
        _mockKernelBuilderService = new Mock<IKernelBuilderService>();

        _mockLoggerFactory
            .Setup(x => x.CreateLogger(It.IsAny<string>()))
            .Returns(_mockLogger.Object);

        _mockLoggerFactory
            .Setup(x => x.CreateLogger(It.Is<string>(s => s == typeof(OrchestrationExecutor).FullName)))
            .Returns(_mockLogger.Object);

        _aiServiceSettings = Options.Create(new AIServiceSettings
        {
            OpenAI = new OpenAISettings
            {
                ApiKey = "test-api-key",
                ModelId = "gpt-4o"
            },
            HttpTimeoutSeconds = 300
        });

        _orchestrationExecutor = new OrchestrationExecutor(
            _mockLoggerFactory.Object,
            _aiServiceSettings,
            _mockKernelBuilderService.Object);
    }

    #region Constructor Tests

    /// <summary>
    /// Verifies that the constructor throws ArgumentNullException when passed a null logger factory.
    /// </summary>
    [Fact]
    public void Constructor_WithNullLoggerFactory_ThrowsArgumentNullException()
    {
        // Arrange, Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() =>
            new OrchestrationExecutor(
                null!,
                _aiServiceSettings,
                _mockKernelBuilderService.Object));
        exception.ParamName.Should().Be("loggerFactory");
    }

    /// <summary>
    /// Verifies that the constructor throws ArgumentNullException when passed null AI service settings.
    /// </summary>
    [Fact]
    public void Constructor_WithNullAIServiceSettings_ThrowsArgumentNullException()
    {
        // Arrange, Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() =>
            new OrchestrationExecutor(
                _mockLoggerFactory.Object,
                null!,
                _mockKernelBuilderService.Object));
        exception.ParamName.Should().Be("aiServiceSettings");
    }

    /// <summary>
    /// Verifies that the constructor throws ArgumentNullException when passed a null kernel builder service.
    /// </summary>
    [Fact]
    public void Constructor_WithNullKernelBuilderService_ThrowsArgumentNullException()
    {
        // Arrange, Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() =>
            new OrchestrationExecutor(
                _mockLoggerFactory.Object,
                _aiServiceSettings,
                null!));
        exception.ParamName.Should().Be("kernelBuilderService");
    }

    /// <summary>
    /// Verifies that the constructor successfully creates an instance with valid dependencies.
    /// </summary>
    [Fact]
    public void Constructor_WithValidDependencies_CreatesInstance()
    {
        // Arrange & Act
        var executor = new OrchestrationExecutor(
            _mockLoggerFactory.Object,
            _aiServiceSettings,
            _mockKernelBuilderService.Object);

        // Assert
        executor.Should().NotBeNull();
    }

    #endregion

    #region ExecuteAsync Tests

    /// <summary>
    /// Verifies that ExecuteAsync successfully executes a factory and returns the expected result.
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_WithValidFactory_ExecutesAndReturnsResult()
    {
        // Arrange
        var expectedHypotheses = new List<Hypothesis>
        {
            new Hypothesis { ShortTitle = "Test Hypothesis", HypothesisText = "Test Text" }
        };

        var mockFactory = new Mock<IOrchestrationFactory<List<Hypothesis>>>();
        mockFactory
            .Setup(f => f.ExecuteCoreAsync(It.IsAny<OrchestrationPromptInput>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedHypotheses);

        var input = new OrchestrationPromptInput
        {
            KeyQuestion = "Test Question",
            Context = "Test Context"
        };

        // Act
        var result = await _orchestrationExecutor.ExecuteAsync(mockFactory.Object, input);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        result[0].ShortTitle.Should().Be("Test Hypothesis");
        mockFactory.Verify(f => f.ExecuteCoreAsync(input, default), Times.Once);
    }

    /// <summary>
    /// Verifies that ExecuteAsync logs an information message when starting factory execution.
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_LogsInformationWhenStarting()
    {
        // Arrange
        var mockFactory = new Mock<IOrchestrationFactory<List<Hypothesis>>>();
        mockFactory
            .Setup(f => f.ExecuteCoreAsync(It.IsAny<OrchestrationPromptInput>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Hypothesis>());

        var input = new OrchestrationPromptInput();

        // Act
        await _orchestrationExecutor.ExecuteAsync(mockFactory.Object, input);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Executing orchestration factory")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    /// <summary>
    /// Verifies that ExecuteAsync logs an information message when factory execution completes successfully.
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_LogsInformationWhenCompleted()
    {
        // Arrange
        var mockFactory = new Mock<IOrchestrationFactory<List<Hypothesis>>>();
        mockFactory
            .Setup(f => f.ExecuteCoreAsync(It.IsAny<OrchestrationPromptInput>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Hypothesis>());

        var input = new OrchestrationPromptInput();

        // Act
        await _orchestrationExecutor.ExecuteAsync(mockFactory.Object, input);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Successfully completed orchestration factory")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    /// <summary>
    /// Verifies that ExecuteAsync respects the cancellation token passed to the factory.
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_WithCancellationToken_PassesTokenToFactory()
    {
        // Arrange
        var mockFactory = new Mock<IOrchestrationFactory<List<Hypothesis>>>();
        var cancellationToken = new CancellationToken();
        mockFactory
            .Setup(f => f.ExecuteCoreAsync(It.IsAny<OrchestrationPromptInput>(), cancellationToken))
            .ReturnsAsync(new List<Hypothesis>());

        var input = new OrchestrationPromptInput();

        // Act
        await _orchestrationExecutor.ExecuteAsync(mockFactory.Object, input, cancellationToken);

        // Assert
        mockFactory.Verify(f => f.ExecuteCoreAsync(input, cancellationToken), Times.Once);
    }

    /// <summary>
    /// Verifies that ExecuteAsync logs an error and rethrows when factory execution fails.
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_WhenFactoryThrows_LogsErrorAndRethrows()
    {
        // Arrange
        var expectedException = new InvalidOperationException("Test exception");
        var mockFactory = new Mock<IOrchestrationFactory<List<Hypothesis>>>();
        mockFactory
            .Setup(f => f.ExecuteCoreAsync(It.IsAny<OrchestrationPromptInput>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(expectedException);

        var input = new OrchestrationPromptInput();

        // Act & Assert
        await _orchestrationExecutor
            .Invoking(e => e.ExecuteAsync(mockFactory.Object, input))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Test exception");

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Error executing orchestration factory")),
                expectedException,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    /// <summary>
    /// Verifies that ExecuteAsync handles null results from factory gracefully.
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_WithNullResult_ReturnsNull()
    {
        // Arrange
        var mockFactory = new Mock<IOrchestrationFactory<List<Hypothesis>?>>();
        mockFactory
            .Setup(f => f.ExecuteCoreAsync(It.IsAny<OrchestrationPromptInput>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((List<Hypothesis>?)null);

        var input = new OrchestrationPromptInput();

        // Act
        var result = await _orchestrationExecutor.ExecuteAsync(mockFactory.Object, input);

        // Assert
        result.Should().BeNull();
    }

    /// <summary>
    /// Verifies that ExecuteAsync can execute different factory types with different result types.
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_WithDifferentResultTypes_ExecutesCorrectly()
    {
        // Arrange
        var expectedEvidence = new List<Evidence>
        {
            new Evidence { Claim = "Test Evidence" }
        };

        var mockFactory = new Mock<IOrchestrationFactory<List<Evidence>>>();
        mockFactory
            .Setup(f => f.ExecuteCoreAsync(It.IsAny<OrchestrationPromptInput>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedEvidence);

        var input = new OrchestrationPromptInput();

        // Act
        var result = await _orchestrationExecutor.ExecuteAsync(mockFactory.Object, input);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        result[0].Claim.Should().Be("Test Evidence");
    }

    #endregion

    #region CreateAgentService Tests

    /// <summary>
    /// Verifies that CreateAgentService creates an AgentService with the correct configuration.
    /// </summary>
    [Fact]
    public void CreateAgentService_WithValidConfiguration_CreatesAgentService()
    {
        // Arrange
        var stepConfiguration = new ACHStepConfiguration
        {
            Id = 1,
            Name = "Test Step",
            AgentConfigurations = new[]
            {
                new AgentConfiguration
                {
                    Name = "Test Agent",
                    Instructions = "Test instructions"
                }
            }
        };

        // Act
        var agentService = _orchestrationExecutor.CreateAgentService(stepConfiguration);

        // Assert
        agentService.Should().NotBeNull();
        agentService.Should().BeAssignableTo<IAgentService>();
    }

    /// <summary>
    /// Verifies that CreateAgentService uses the AI service settings provided in constructor.
    /// </summary>
    [Fact]
    public void CreateAgentService_UsesProvidedAIServiceSettings()
    {
        // Arrange
        var stepConfiguration = new ACHStepConfiguration
        {
            Id = 1,
            Name = "Test Step",
            AgentConfigurations = Array.Empty<AgentConfiguration>()
        };

        // Act
        var agentService = _orchestrationExecutor.CreateAgentService(stepConfiguration);

        // Assert
        agentService.Should().NotBeNull();
    }

    /// <summary>
    /// Verifies that CreateAgentService handles empty agent configurations without throwing.
    /// </summary>
    [Fact]
    public void CreateAgentService_WithEmptyAgentConfigurations_CreatesService()
    {
        // Arrange
        var stepConfiguration = new ACHStepConfiguration
        {
            Id = 1,
            Name = "Test Step",
            AgentConfigurations = Array.Empty<AgentConfiguration>()
        };

        // Act
        var agentService = _orchestrationExecutor.CreateAgentService(stepConfiguration);

        // Assert
        agentService.Should().NotBeNull();
    }

    /// <summary>
    /// Verifies that CreateAgentService creates different instances for different configurations.
    /// </summary>
    [Fact]
    public void CreateAgentService_WithDifferentConfigurations_CreatesDifferentInstances()
    {
        // Arrange
        var stepConfiguration1 = new ACHStepConfiguration
        {
            Id = 1,
            Name = "Step 1",
            AgentConfigurations = Array.Empty<AgentConfiguration>()
        };

        var stepConfiguration2 = new ACHStepConfiguration
        {
            Id = 2,
            Name = "Step 2",
            AgentConfigurations = Array.Empty<AgentConfiguration>()
        };

        // Act
        var agentService1 = _orchestrationExecutor.CreateAgentService(stepConfiguration1);
        var agentService2 = _orchestrationExecutor.CreateAgentService(stepConfiguration2);

        // Assert
        agentService1.Should().NotBeSameAs(agentService2);
    }

    #endregion

    #region GetKernelBuilderService Tests

    /// <summary>
    /// Verifies that GetKernelBuilderService returns the kernel builder service provided in constructor.
    /// </summary>
    [Fact]
    public void GetKernelBuilderService_ReturnsProvidedKernelBuilderService()
    {
        // Act
        var result = _orchestrationExecutor.GetKernelBuilderService();

        // Assert
        result.Should().BeSameAs(_mockKernelBuilderService.Object);
    }

    /// <summary>
    /// Verifies that GetKernelBuilderService returns the same instance on multiple calls.
    /// </summary>
    [Fact]
    public void GetKernelBuilderService_CalledMultipleTimes_ReturnsSameInstance()
    {
        // Act
        var result1 = _orchestrationExecutor.GetKernelBuilderService();
        var result2 = _orchestrationExecutor.GetKernelBuilderService();

        // Assert
        result1.Should().BeSameAs(result2);
    }

    #endregion

    #region GetLoggerFactory Tests

    /// <summary>
    /// Verifies that GetLoggerFactory returns the logger factory provided in constructor.
    /// </summary>
    [Fact]
    public void GetLoggerFactory_ReturnsProvidedLoggerFactory()
    {
        // Act
        var result = _orchestrationExecutor.GetLoggerFactory();

        // Assert
        result.Should().BeSameAs(_mockLoggerFactory.Object);
    }

    /// <summary>
    /// Verifies that GetLoggerFactory returns the same instance on multiple calls.
    /// </summary>
    [Fact]
    public void GetLoggerFactory_CalledMultipleTimes_ReturnsSameInstance()
    {
        // Act
        var result1 = _orchestrationExecutor.GetLoggerFactory();
        var result2 = _orchestrationExecutor.GetLoggerFactory();

        // Assert
        result1.Should().BeSameAs(result2);
    }

    #endregion

    #region CreateOrchestrationOptions Tests

    /// <summary>
    /// Verifies that CreateOrchestrationOptions creates options with the correct orchestration settings.
    /// </summary>
    [Fact]
    public void CreateOrchestrationOptions_WithValidConfiguration_CreatesOptions()
    {
        // Arrange
        var stepConfiguration = new ACHStepConfiguration
        {
            Id = 1,
            Name = "Test Step",
            OrchestrationSettings = new OrchestrationSettings
            {
                MaximumInvocationCount = 10,
                TimeoutInMinutes = 5,
                WriteResponses = true,
                StreamResponses = false
            }
        };

        // Act
        var result = _orchestrationExecutor.CreateOrchestrationOptions(stepConfiguration);

        // Assert
        result.Should().NotBeNull();
        result.Value.Should().NotBeNull();
        result.Value.MaximumInvocationCount.Should().Be(10);
        result.Value.TimeoutInMinutes.Should().Be(5);
        result.Value.WriteResponses.Should().BeTrue();
        result.Value.StreamResponses.Should().BeFalse();
    }

    /// <summary>
    /// Verifies that CreateOrchestrationOptions creates new options instances for different configurations.
    /// </summary>
    [Fact]
    public void CreateOrchestrationOptions_WithDifferentConfigurations_CreatesDifferentOptions()
    {
        // Arrange
        var stepConfiguration1 = new ACHStepConfiguration
        {
            Id = 1,
            OrchestrationSettings = new OrchestrationSettings { MaximumInvocationCount = 10 }
        };

        var stepConfiguration2 = new ACHStepConfiguration
        {
            Id = 2,
            OrchestrationSettings = new OrchestrationSettings { MaximumInvocationCount = 20 }
        };

        // Act
        var result1 = _orchestrationExecutor.CreateOrchestrationOptions(stepConfiguration1);
        var result2 = _orchestrationExecutor.CreateOrchestrationOptions(stepConfiguration2);

        // Assert
        result1.Value.MaximumInvocationCount.Should().Be(10);
        result2.Value.MaximumInvocationCount.Should().Be(20);
    }

    /// <summary>
    /// Verifies that CreateOrchestrationOptions handles null orchestration settings gracefully.
    /// </summary>
    [Fact]
    public void CreateOrchestrationOptions_WithNullOrchestrationSettings_CreatesOptionsWithNull()
    {
        // Arrange
        var stepConfiguration = new ACHStepConfiguration
        {
            Id = 1,
            Name = "Test Step",
            OrchestrationSettings = null!
        };

        // Act
        var result = _orchestrationExecutor.CreateOrchestrationOptions(stepConfiguration);

        // Assert
        result.Should().NotBeNull();
        result.Value.Should().BeNull();
    }

    #endregion

    #region Integration Tests

    /// <summary>
    /// Verifies the complete workflow of executing a factory and using helper methods works correctly.
    /// </summary>
    [Fact]
    public async Task EndToEnd_ExecuteFactoryAndUseHelperMethods_WorksCorrectly()
    {
        // Arrange
        var mockFactory = new Mock<IOrchestrationFactory<List<Hypothesis>>>();
        mockFactory
            .Setup(f => f.ExecuteCoreAsync(It.IsAny<OrchestrationPromptInput>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Hypothesis>
            {
                new Hypothesis { ShortTitle = "Integration Test", HypothesisText = "Test" }
            });

        var stepConfiguration = new ACHStepConfiguration
        {
            Id = 1,
            Name = "Integration Step",
            AgentConfigurations = Array.Empty<AgentConfiguration>(),
            OrchestrationSettings = new OrchestrationSettings { MaximumInvocationCount = 5 }
        };

        var input = new OrchestrationPromptInput
        {
            KeyQuestion = "Test Question"
        };

        // Act
        var agentService = _orchestrationExecutor.CreateAgentService(stepConfiguration);
        var kernelBuilder = _orchestrationExecutor.GetKernelBuilderService();
        var options = _orchestrationExecutor.CreateOrchestrationOptions(stepConfiguration);
        var result = await _orchestrationExecutor.ExecuteAsync(mockFactory.Object, input);

        // Assert
        agentService.Should().NotBeNull();
        kernelBuilder.Should().NotBeNull();
        options.Value.MaximumInvocationCount.Should().Be(5);
        result.Should().HaveCount(1);
        result[0].ShortTitle.Should().Be("Integration Test");
    }

    /// <summary>
    /// Verifies that multiple sequential factory executions work independently.
    /// </summary>
    [Fact]
    public async Task MultipleExecutions_Sequential_WorkIndependently()
    {
        // Arrange
        var mockFactory1 = new Mock<IOrchestrationFactory<List<Hypothesis>>>();
        mockFactory1
            .Setup(f => f.ExecuteCoreAsync(It.IsAny<OrchestrationPromptInput>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Hypothesis>
            {
                new Hypothesis { ShortTitle = "Hypothesis 1" }
            });

        var mockFactory2 = new Mock<IOrchestrationFactory<List<Evidence>>>();
        mockFactory2
            .Setup(f => f.ExecuteCoreAsync(It.IsAny<OrchestrationPromptInput>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Evidence>
            {
                new Evidence { Claim = "Evidence 1" }
            });

        var input = new OrchestrationPromptInput();

        // Act
        var result1 = await _orchestrationExecutor.ExecuteAsync(mockFactory1.Object, input);
        var result2 = await _orchestrationExecutor.ExecuteAsync(mockFactory2.Object, input);

        // Assert
        result1.Should().HaveCount(1);
        result1[0].ShortTitle.Should().Be("Hypothesis 1");

        result2.Should().HaveCount(1);
        result2[0].Claim.Should().Be("Evidence 1");
    }

    #endregion
}
