using System.Net.Http;
using Microsoft.Extensions.Logging;
using Moq;
using NIU.ACH_AI.Application.Configuration;
using NIU.ACH_AI.Application.Interfaces;
using NIU.ACH_AI.Infrastructure.AI.Services;
using NIU.ACH_AI.Infrastructure.Configuration;

namespace NIU.ACH_AI.Infrastructure.Tests.AI.Services;

/// <summary>
/// Unit tests for AgentService.
///
/// Testing Strategy:
/// -----------------
/// AgentService creates ChatCompletionAgents from configuration and builds kernels
/// using different AI providers (OpenAI, Azure, Ollama). Key testing areas:
///
/// 1. Constructor - Proper initialization of dependencies
/// 2. CreateAgents - Agent creation from configurations
/// 3. Provider Selection - ServiceId routing to correct adapter
/// 4. Configuration Validation - Error handling for missing/invalid configs
/// 5. Model Override - ModelId override behavior
///
/// Note: We cannot fully test BuildKernel as it creates real Semantic Kernel objects.
/// We focus on testing the logic paths and error handling.
/// </summary>
public class AgentServiceTests
{
    #region Test Infrastructure

    private static (AgentService Service,
        Mock<ILoggerFactory> LoggerFactoryMock,
        Mock<ILogger<AgentService>> LoggerMock) CreateService(
        IEnumerable<AgentConfiguration> agentConfigurations,
        AIServiceSettings aiServiceSettings)
    {
        var loggerFactoryMock = new Mock<ILoggerFactory>();
        var loggerMock = new Mock<ILogger<AgentService>>();

        loggerFactoryMock
            .Setup(f => f.CreateLogger(It.IsAny<string>()))
            .Returns(loggerMock.Object);

        var httpClientFactoryMock = new Mock<IHttpClientFactory>();
        httpClientFactoryMock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(new HttpClient());

        var service = new AgentService(
            agentConfigurations,
            aiServiceSettings,
            loggerFactoryMock.Object,
            httpClientFactoryMock.Object);

        return (service, loggerFactoryMock, loggerMock);
    }

    private static AIServiceSettings CreateValidOpenAISettings()
    {
        return new AIServiceSettings
        {
            OpenAI = new OpenAISettings
            {
                ApiKey = "test-api-key",
                ModelId = "gpt-4o"
            }
        };
    }

    private static AIServiceSettings CreateValidAzureSettings()
    {
        return new AIServiceSettings
        {
            AzureOpenAI = new AzureOpenAISettings
            {
                ApiKey = "test-api-key",
                Endpoint = "https://test.openai.azure.com",
                DeploymentName = "test-deployment",
                ModelId = "gpt-4"
            }
        };
    }

    private static AIServiceSettings CreateValidOllamaSettings()
    {
        return new AIServiceSettings
        {
            Ollama = new OllamaSettings
            {
                Endpoint = "http://localhost:11434",
                ModelId = "llama2"
            }
        };
    }

    private static AgentConfiguration CreateAgentConfig(
        string name = "TestAgent",
        string? serviceId = null,
        string? modelId = null)
    {
        return new AgentConfiguration
        {
            Name = name,
            Description = $"Description for {name}",
            Instructions = $"Instructions for {name}",
            ServiceId = serviceId,
            ModelId = modelId
        };
    }

    #endregion

    #region Constructor Tests

    /// <summary>
    /// WHY: Verifies service can be instantiated with valid dependencies.
    /// </summary>
    [Fact]
    public void Constructor_WithValidDependencies_CreatesInstance()
    {
        // Arrange
        var configs = new List<AgentConfiguration> { CreateAgentConfig() };
        var settings = CreateValidOpenAISettings();

        // Act
        var (service, _, _) = CreateService(configs, settings);

        // Assert
        Assert.NotNull(service);
    }

    /// <summary>
    /// WHY: Verifies the service creates a typed logger on construction.
    /// </summary>
    [Fact]
    public void Constructor_Always_CreatesTypedLogger()
    {
        // Arrange
        var loggerFactoryMock = new Mock<ILoggerFactory>();
        var loggerMock = new Mock<ILogger>();
        string? capturedCategory = null;

        loggerFactoryMock
            .Setup(f => f.CreateLogger(It.IsAny<string>()))
            .Callback<string>(category => capturedCategory = category)
            .Returns(loggerMock.Object);

        var configs = new List<AgentConfiguration>();
        var settings = CreateValidOpenAISettings();

        // Act
        var service = new AgentService(configs, settings, loggerFactoryMock.Object, Mock.Of<IHttpClientFactory>());

        // Assert
        Assert.NotNull(capturedCategory);
        Assert.Contains("AgentService", capturedCategory);
    }

    #endregion

    #region CreateAgents Tests

    /// <summary>
    /// WHY: Verifies agents are created for each configuration.
    /// </summary>
    [Fact]
    public void CreateAgents_WithMultipleConfigs_CreatesCorrectNumberOfAgents()
    {
        // Arrange
        var configs = new List<AgentConfiguration>
        {
            CreateAgentConfig("Agent1"),
            CreateAgentConfig("Agent2"),
            CreateAgentConfig("Agent3")
        };
        var settings = CreateValidOpenAISettings();
        var (service, _, _) = CreateService(configs, settings);

        // Act
        var agents = ((IAgentService)service).CreateAgents();

        // Assert
        Assert.Equal(3, agents.Count());
    }

    /// <summary>
    /// WHY: Verifies empty configuration list produces no agents.
    /// </summary>
    [Fact]
    public void CreateAgents_WithEmptyConfigs_ReturnsEmptyCollection()
    {
        // Arrange
        var configs = new List<AgentConfiguration>();
        var settings = CreateValidOpenAISettings();
        var (service, _, _) = CreateService(configs, settings);

        // Act
        var agents = ((IAgentService)service).CreateAgents();

        // Assert
        Assert.Empty(agents);
    }

    /// <summary>
    /// WHY: Verifies agent names match configuration names.
    /// </summary>
    [Fact]
    public void CreateAgents_Always_PreservesAgentNames()
    {
        // Arrange
        var configs = new List<AgentConfiguration>
        {
            CreateAgentConfig("AlphaAgent"),
            CreateAgentConfig("BetaAgent")
        };
        var settings = CreateValidOpenAISettings();
        var (service, _, _) = CreateService(configs, settings);

        // Act
        var agents = ((IAgentService)service).CreateAgents().ToList();

        // Assert
        Assert.Contains(agents, a => a.Name == "AlphaAgent");
        Assert.Contains(agents, a => a.Name == "BetaAgent");
    }

    /// <summary>
    /// WHY: Verifies agent descriptions are preserved from configuration.
    /// </summary>
    [Fact]
    public void CreateAgents_Always_PreservesAgentDescriptions()
    {
        // Arrange
        var config = new AgentConfiguration
        {
            Name = "TestAgent",
            Description = "Custom description for testing",
            Instructions = "Some instructions"
        };
        var settings = CreateValidOpenAISettings();
        var (service, _, _) = CreateService(new[] { config }, settings);

        // Act
        var agents = ((IAgentService)service).CreateAgents().ToList();

        // Assert
        Assert.Single(agents);
        Assert.Equal("Custom description for testing", agents[0].Description);
    }

    /// <summary>
    /// WHY: Verifies agent instructions are preserved from configuration.
    /// </summary>
    [Fact]
    public void CreateAgents_Always_PreservesAgentInstructions()
    {
        // Arrange
        var config = new AgentConfiguration
        {
            Name = "TestAgent",
            Description = "Description",
            Instructions = "You are an expert analyst. Follow these guidelines..."
        };
        var settings = CreateValidOpenAISettings();
        var (service, _, _) = CreateService(new[] { config }, settings);

        // Act
        var agents = ((IAgentService)service).CreateAgents().ToList();

        // Assert
        Assert.Single(agents);
        Assert.Equal("You are an expert analyst. Follow these guidelines...", agents[0].Instructions);
    }

    #endregion

    #region Provider Selection Tests - OpenAI

    /// <summary>
    /// WHY: Verifies default provider is OpenAI when no ServiceId is specified.
    /// </summary>
    [Fact]
    public void CreateAgents_WithNoServiceId_DefaultsToOpenAI()
    {
        // Arrange
        var config = CreateAgentConfig("TestAgent", serviceId: null);
        var settings = CreateValidOpenAISettings();
        var (service, _, _) = CreateService(new[] { config }, settings);

        // Act - Should not throw since OpenAI is configured
        var agents = ((IAgentService)service).CreateAgents();

        // Assert
        Assert.Single(agents);
    }

    /// <summary>
    /// WHY: Verifies empty string ServiceId defaults to OpenAI.
    /// </summary>
    [Fact]
    public void CreateAgents_WithEmptyServiceId_DefaultsToOpenAI()
    {
        // Arrange
        var config = CreateAgentConfig("TestAgent", serviceId: "");
        var settings = CreateValidOpenAISettings();
        var (service, _, _) = CreateService(new[] { config }, settings);

        // Act
        var agents = ((IAgentService)service).CreateAgents();

        // Assert
        Assert.Single(agents);
    }

    /// <summary>
    /// WHY: Verifies whitespace ServiceId defaults to OpenAI.
    /// </summary>
    [Fact]
    public void CreateAgents_WithWhitespaceServiceId_DefaultsToOpenAI()
    {
        // Arrange
        var config = CreateAgentConfig("TestAgent", serviceId: "   ");
        var settings = CreateValidOpenAISettings();
        var (service, _, _) = CreateService(new[] { config }, settings);

        // Act
        var agents = ((IAgentService)service).CreateAgents();

        // Assert
        Assert.Single(agents);
    }

    /// <summary>
    /// WHY: Verifies explicit "openai" ServiceId works correctly.
    /// </summary>
    [Fact]
    public void CreateAgents_WithOpenAIServiceId_UsesOpenAIAdapter()
    {
        // Arrange
        var config = CreateAgentConfig("TestAgent", serviceId: "openai");
        var settings = CreateValidOpenAISettings();
        var (service, _, _) = CreateService(new[] { config }, settings);

        // Act
        var agents = ((IAgentService)service).CreateAgents();

        // Assert
        Assert.Single(agents);
    }

    /// <summary>
    /// WHY: Verifies case-insensitive ServiceId matching for OpenAI.
    /// </summary>
    [Theory]
    [InlineData("OpenAI")]
    [InlineData("OPENAI")]
    [InlineData("OpenAi")]
    public void CreateAgents_WithOpenAIServiceIdCaseVariations_UsesOpenAIAdapter(string serviceId)
    {
        // Arrange
        var config = CreateAgentConfig("TestAgent", serviceId: serviceId);
        var settings = CreateValidOpenAISettings();
        var (service, _, _) = CreateService(new[] { config }, settings);

        // Act
        var agents = ((IAgentService)service).CreateAgents();

        // Assert
        Assert.Single(agents);
    }

    #endregion

    #region Provider Selection Tests - Azure

    /// <summary>
    /// WHY: Verifies "azure" ServiceId routes to Azure adapter.
    /// </summary>
    [Fact]
    public void CreateAgents_WithAzureServiceId_UsesAzureAdapter()
    {
        // Arrange
        var config = CreateAgentConfig("TestAgent", serviceId: "azure");
        var settings = CreateValidAzureSettings();
        var (service, _, _) = CreateService(new[] { config }, settings);

        // Act
        var agents = ((IAgentService)service).CreateAgents();

        // Assert
        Assert.Single(agents);
    }

    /// <summary>
    /// WHY: Verifies case-insensitive ServiceId matching for Azure.
    /// </summary>
    [Theory]
    [InlineData("Azure")]
    [InlineData("AZURE")]
    [InlineData("AzUrE")]
    public void CreateAgents_WithAzureServiceIdCaseVariations_UsesAzureAdapter(string serviceId)
    {
        // Arrange
        var config = CreateAgentConfig("TestAgent", serviceId: serviceId);
        var settings = CreateValidAzureSettings();
        var (service, _, _) = CreateService(new[] { config }, settings);

        // Act
        var agents = ((IAgentService)service).CreateAgents();

        // Assert
        Assert.Single(agents);
    }

    #endregion

    #region Provider Selection Tests - Ollama

    /// <summary>
    /// WHY: Verifies "ollama" ServiceId routes to Ollama adapter.
    /// </summary>
    [Fact]
    public void CreateAgents_WithOllamaServiceId_UsesOllamaAdapter()
    {
        // Arrange
        var config = CreateAgentConfig("TestAgent", serviceId: "ollama");
        var settings = CreateValidOllamaSettings();
        var (service, _, _) = CreateService(new[] { config }, settings);

        // Act
        var agents = ((IAgentService)service).CreateAgents();

        // Assert
        Assert.Single(agents);
    }

    /// <summary>
    /// WHY: Verifies case-insensitive ServiceId matching for Ollama.
    /// </summary>
    [Theory]
    [InlineData("Ollama")]
    [InlineData("OLLAMA")]
    [InlineData("OlLaMa")]
    public void CreateAgents_WithOllamaServiceIdCaseVariations_UsesOllamaAdapter(string serviceId)
    {
        // Arrange
        var config = CreateAgentConfig("TestAgent", serviceId: serviceId);
        var settings = CreateValidOllamaSettings();
        var (service, _, _) = CreateService(new[] { config }, settings);

        // Act
        var agents = ((IAgentService)service).CreateAgents();

        // Assert
        Assert.Single(agents);
    }

    #endregion

    #region Unsupported Provider Tests

    /// <summary>
    /// WHY: Verifies InvalidOperationException for unsupported ServiceId.
    /// </summary>
    [Fact]
    public void CreateAgents_WithUnsupportedServiceId_ThrowsInvalidOperationException()
    {
        // Arrange
        var config = CreateAgentConfig("TestAgent", serviceId: "unsupported");
        var settings = CreateValidOpenAISettings();
        var (service, _, _) = CreateService(new[] { config }, settings);

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() =>
            ((IAgentService)service).CreateAgents().ToList());
        Assert.Contains("Unsupported ServiceId", exception.Message);
        Assert.Contains("unsupported", exception.Message);
    }

    /// <summary>
    /// WHY: Verifies error message includes valid options.
    /// </summary>
    [Fact]
    public void CreateAgents_WithUnsupportedServiceId_ErrorMessageIncludesValidOptions()
    {
        // Arrange
        var config = CreateAgentConfig("TestAgent", serviceId: "invalid");
        var settings = CreateValidOpenAISettings();
        var (service, _, _) = CreateService(new[] { config }, settings);

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() =>
            ((IAgentService)service).CreateAgents().ToList());
        Assert.Contains("openai", exception.Message);
        Assert.Contains("azure", exception.Message);
        Assert.Contains("ollama", exception.Message);
    }

    #endregion

    #region Missing Configuration Tests - OpenAI

    /// <summary>
    /// WHY: Verifies error when OpenAI settings are null.
    /// </summary>
    [Fact]
    public void CreateAgents_WithNullOpenAISettings_ThrowsInvalidOperationException()
    {
        // Arrange
        var config = CreateAgentConfig("TestAgent", serviceId: "openai");
        var settings = new AIServiceSettings { OpenAI = null };
        var (service, _, _) = CreateService(new[] { config }, settings);

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() =>
            ((IAgentService)service).CreateAgents().ToList());
        Assert.Contains("OpenAI", exception.Message);
        Assert.Contains("not configured", exception.Message);
    }

    /// <summary>
    /// WHY: Verifies error when OpenAI API key is empty.
    /// </summary>
    [Fact]
    public void CreateAgents_WithEmptyOpenAIApiKey_ThrowsInvalidOperationException()
    {
        // Arrange
        var config = CreateAgentConfig("TestAgent", serviceId: "openai");
        var settings = new AIServiceSettings
        {
            OpenAI = new OpenAISettings { ApiKey = "" }
        };
        var (service, _, _) = CreateService(new[] { config }, settings);

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() =>
            ((IAgentService)service).CreateAgents().ToList());
        Assert.Contains("OpenAI", exception.Message);
    }

    /// <summary>
    /// WHY: Verifies error when OpenAI API key is whitespace.
    /// </summary>
    [Fact]
    public void CreateAgents_WithWhitespaceOpenAIApiKey_ThrowsInvalidOperationException()
    {
        // Arrange
        var config = CreateAgentConfig("TestAgent", serviceId: "openai");
        var settings = new AIServiceSettings
        {
            OpenAI = new OpenAISettings { ApiKey = "   " }
        };
        var (service, _, _) = CreateService(new[] { config }, settings);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() =>
            ((IAgentService)service).CreateAgents().ToList());
    }

    #endregion

    #region Missing Configuration Tests - Azure

    /// <summary>
    /// WHY: Verifies error when Azure settings are null.
    /// </summary>
    [Fact]
    public void CreateAgents_WithNullAzureSettings_ThrowsInvalidOperationException()
    {
        // Arrange
        var config = CreateAgentConfig("TestAgent", serviceId: "azure");
        var settings = new AIServiceSettings { AzureOpenAI = null };
        var (service, _, _) = CreateService(new[] { config }, settings);

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() =>
            ((IAgentService)service).CreateAgents().ToList());
        Assert.Contains("Azure OpenAI", exception.Message);
    }

    /// <summary>
    /// WHY: Verifies error when Azure API key is missing.
    /// </summary>
    [Fact]
    public void CreateAgents_WithEmptyAzureApiKey_ThrowsInvalidOperationException()
    {
        // Arrange
        var config = CreateAgentConfig("TestAgent", serviceId: "azure");
        var settings = new AIServiceSettings
        {
            AzureOpenAI = new AzureOpenAISettings
            {
                ApiKey = "",
                Endpoint = "https://test.openai.azure.com",
                DeploymentName = "test-deployment"
            }
        };
        var (service, _, _) = CreateService(new[] { config }, settings);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() =>
            ((IAgentService)service).CreateAgents().ToList());
    }

    /// <summary>
    /// WHY: Verifies error when Azure Endpoint is missing.
    /// </summary>
    [Fact]
    public void CreateAgents_WithEmptyAzureEndpoint_ThrowsInvalidOperationException()
    {
        // Arrange
        var config = CreateAgentConfig("TestAgent", serviceId: "azure");
        var settings = new AIServiceSettings
        {
            AzureOpenAI = new AzureOpenAISettings
            {
                ApiKey = "test-key",
                Endpoint = "",
                DeploymentName = "test-deployment"
            }
        };
        var (service, _, _) = CreateService(new[] { config }, settings);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() =>
            ((IAgentService)service).CreateAgents().ToList());
    }

    /// <summary>
    /// WHY: Verifies error when Azure DeploymentName is missing.
    /// </summary>
    [Fact]
    public void CreateAgents_WithEmptyAzureDeploymentName_ThrowsInvalidOperationException()
    {
        // Arrange
        var config = CreateAgentConfig("TestAgent", serviceId: "azure");
        var settings = new AIServiceSettings
        {
            AzureOpenAI = new AzureOpenAISettings
            {
                ApiKey = "test-key",
                Endpoint = "https://test.openai.azure.com",
                DeploymentName = ""
            }
        };
        var (service, _, _) = CreateService(new[] { config }, settings);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() =>
            ((IAgentService)service).CreateAgents().ToList());
    }

    #endregion

    #region Missing Configuration Tests - Ollama

    /// <summary>
    /// WHY: Verifies error when Ollama settings are null.
    /// </summary>
    [Fact]
    public void CreateAgents_WithNullOllamaSettings_ThrowsInvalidOperationException()
    {
        // Arrange
        var config = CreateAgentConfig("TestAgent", serviceId: "ollama");
        var settings = new AIServiceSettings { Ollama = null };
        var (service, _, _) = CreateService(new[] { config }, settings);

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() =>
            ((IAgentService)service).CreateAgents().ToList());
        Assert.Contains("Ollama", exception.Message);
    }

    /// <summary>
    /// WHY: Verifies error when Ollama Endpoint is missing.
    /// </summary>
    [Fact]
    public void CreateAgents_WithEmptyOllamaEndpoint_ThrowsInvalidOperationException()
    {
        // Arrange
        var config = CreateAgentConfig("TestAgent", serviceId: "ollama");
        var settings = new AIServiceSettings
        {
            Ollama = new OllamaSettings
            {
                Endpoint = "",
                ModelId = "llama2"
            }
        };
        var (service, _, _) = CreateService(new[] { config }, settings);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() =>
            ((IAgentService)service).CreateAgents().ToList());
    }

    /// <summary>
    /// WHY: Verifies error when Ollama ModelId is missing.
    /// </summary>
    [Fact]
    public void CreateAgents_WithEmptyOllamaModelId_ThrowsInvalidOperationException()
    {
        // Arrange
        var config = CreateAgentConfig("TestAgent", serviceId: "ollama");
        var settings = new AIServiceSettings
        {
            Ollama = new OllamaSettings
            {
                Endpoint = "http://localhost:11434",
                ModelId = ""
            }
        };
        var (service, _, _) = CreateService(new[] { config }, settings);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() =>
            ((IAgentService)service).CreateAgents().ToList());
    }

    #endregion

    #region Mixed Provider Tests

    /// <summary>
    /// WHY: Verifies multiple agents can use different providers.
    /// </summary>
    [Fact]
    public void CreateAgents_WithMixedProviders_CreatesAllAgents()
    {
        // Arrange
        var configs = new List<AgentConfiguration>
        {
            CreateAgentConfig("OpenAIAgent", serviceId: "openai"),
            CreateAgentConfig("AzureAgent", serviceId: "azure"),
            CreateAgentConfig("OllamaAgent", serviceId: "ollama")
        };
        var settings = new AIServiceSettings
        {
            OpenAI = new OpenAISettings { ApiKey = "test-key", ModelId = "gpt-4o" },
            AzureOpenAI = new AzureOpenAISettings
            {
                ApiKey = "test-key",
                Endpoint = "https://test.openai.azure.com",
                DeploymentName = "test-deployment"
            },
            Ollama = new OllamaSettings
            {
                Endpoint = "http://localhost:11434",
                ModelId = "llama2"
            }
        };
        var (service, _, _) = CreateService(configs, settings);

        // Act
        var agents = ((IAgentService)service).CreateAgents().ToList();

        // Assert
        Assert.Equal(3, agents.Count);
        Assert.Contains(agents, a => a.Name == "OpenAIAgent");
        Assert.Contains(agents, a => a.Name == "AzureAgent");
        Assert.Contains(agents, a => a.Name == "OllamaAgent");
    }

    #endregion

    #region Interface Implementation Tests

    /// <summary>
    /// WHY: Verifies service implements IAgentService interface.
    /// </summary>
    [Fact]
    public void Service_ImplementsIAgentService()
    {
        // Arrange
        var configs = new List<AgentConfiguration>();
        var settings = CreateValidOpenAISettings();
        var (service, _, _) = CreateService(configs, settings);

        // Assert
        Assert.IsAssignableFrom<IAgentService>(service);
    }

    #endregion
}
