using System.Net.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using NIU.ACH_AI.Application.Configuration;
using NIU.ACH_AI.Application.Interfaces;
using NIU.ACH_AI.Infrastructure.AI.Services;
using NIU.ACH_AI.Infrastructure.Configuration;

namespace NIU.ACH_AI.Infrastructure.Tests.AI.Services;

/// <summary>
/// Unit tests for KernelBuilderService.
///
/// Testing Strategy:
/// -----------------
/// KernelBuilderService builds Semantic Kernel instances for orchestration purposes.
/// It implements a provider fallback chain: OpenAI > Azure OpenAI > Ollama.
///
/// Key testing areas:
/// 1. Constructor - Proper initialization
/// 2. BuildKernel - Provider selection priority
/// 3. CurrentProvider - Property assignment based on selected provider
/// 4. Error handling - When no provider is configured
/// </summary>
public class KernelBuilderServiceTests
{
    #region Test Infrastructure

    private static (KernelBuilderService Service,
        Mock<ILoggerFactory> LoggerFactoryMock) CreateService(AIServiceSettings settings)
    {
        var loggerFactoryMock = new Mock<ILoggerFactory>();
        var loggerMock = new Mock<ILogger>();

        loggerFactoryMock
            .Setup(f => f.CreateLogger(It.IsAny<string>()))
            .Returns(loggerMock.Object);

        var httpClientFactoryMock = new Mock<IHttpClientFactory>();
        httpClientFactoryMock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(new HttpClient());

        var optionsMock = new Mock<IOptions<AIServiceSettings>>();
        optionsMock.Setup(o => o.Value).Returns(settings);

        var service = new KernelBuilderService(optionsMock.Object, loggerFactoryMock.Object, httpClientFactoryMock.Object);

        return (service, loggerFactoryMock);
    }

    private static AIServiceSettings CreateSettingsWithOpenAI()
    {
        return new AIServiceSettings
        {
            OpenAI = new OpenAISettings
            {
                ApiKey = "test-openai-key",
                ModelId = "gpt-4o"
            }
        };
    }

    private static AIServiceSettings CreateSettingsWithAzure()
    {
        return new AIServiceSettings
        {
            AzureOpenAI = new AzureOpenAISettings
            {
                ApiKey = "test-azure-key",
                Endpoint = "https://test.openai.azure.com",
                DeploymentName = "test-deployment",
                ModelId = "gpt-4"
            }
        };
    }

    private static AIServiceSettings CreateSettingsWithOllama()
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

    private static AIServiceSettings CreateSettingsWithAllProviders()
    {
        return new AIServiceSettings
        {
            OpenAI = new OpenAISettings
            {
                ApiKey = "test-openai-key",
                ModelId = "gpt-4o"
            },
            AzureOpenAI = new AzureOpenAISettings
            {
                ApiKey = "test-azure-key",
                Endpoint = "https://test.openai.azure.com",
                DeploymentName = "test-deployment"
            },
            Ollama = new OllamaSettings
            {
                Endpoint = "http://localhost:11434",
                ModelId = "llama2"
            }
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
        // Arrange & Act
        var (service, _) = CreateService(CreateSettingsWithOpenAI());

        // Assert
        Assert.NotNull(service);
    }

    /// <summary>
    /// WHY: Verifies service implements IKernelBuilderService interface.
    /// </summary>
    [Fact]
    public void Service_ImplementsIKernelBuilderService()
    {
        // Arrange
        var (service, _) = CreateService(CreateSettingsWithOpenAI());

        // Assert
        Assert.IsAssignableFrom<IKernelBuilderService>(service);
    }

    #endregion

    #region BuildKernel - OpenAI Priority Tests

    /// <summary>
    /// WHY: Verifies OpenAI is selected when it's the only provider configured.
    /// </summary>
    [Fact]
    public void BuildKernel_WithOnlyOpenAI_SelectsOpenAI()
    {
        // Arrange
        var (service, _) = CreateService(CreateSettingsWithOpenAI());

        // Act
        var kernel = service.BuildKernel();

        // Assert
        Assert.NotNull(kernel);
        Assert.Equal(AIServiceProvider.OpenAI, service.CurrentProvider);
    }

    /// <summary>
    /// WHY: Verifies OpenAI is prioritized when all providers are configured.
    /// </summary>
    [Fact]
    public void BuildKernel_WithAllProviders_PrioritizesOpenAI()
    {
        // Arrange
        var (service, _) = CreateService(CreateSettingsWithAllProviders());

        // Act
        var kernel = service.BuildKernel();

        // Assert
        Assert.NotNull(kernel);
        Assert.Equal(AIServiceProvider.OpenAI, service.CurrentProvider);
    }

    #endregion

    #region BuildKernel - Azure Fallback Tests

    /// <summary>
    /// WHY: Verifies Azure is selected when it's the only provider configured.
    /// </summary>
    [Fact]
    public void BuildKernel_WithOnlyAzure_SelectsAzure()
    {
        // Arrange
        var (service, _) = CreateService(CreateSettingsWithAzure());

        // Act
        var kernel = service.BuildKernel();

        // Assert
        Assert.NotNull(kernel);
        Assert.Equal(AIServiceProvider.AzureOpenAI, service.CurrentProvider);
    }

    /// <summary>
    /// WHY: Verifies Azure is used when OpenAI is not configured.
    /// </summary>
    [Fact]
    public void BuildKernel_WithAzureAndOllama_SelectsAzure()
    {
        // Arrange
        var settings = new AIServiceSettings
        {
            AzureOpenAI = new AzureOpenAISettings
            {
                ApiKey = "test-azure-key",
                Endpoint = "https://test.openai.azure.com",
                DeploymentName = "test-deployment"
            },
            Ollama = new OllamaSettings
            {
                Endpoint = "http://localhost:11434",
                ModelId = "llama2"
            }
        };
        var (service, _) = CreateService(settings);

        // Act
        var kernel = service.BuildKernel();

        // Assert
        Assert.NotNull(kernel);
        Assert.Equal(AIServiceProvider.AzureOpenAI, service.CurrentProvider);
    }

    /// <summary>
    /// WHY: Verifies Azure is skipped when API key is missing.
    /// </summary>
    [Fact]
    public void BuildKernel_WithAzureMissingApiKey_FallsToOllama()
    {
        // Arrange
        var settings = new AIServiceSettings
        {
            AzureOpenAI = new AzureOpenAISettings
            {
                ApiKey = "", // Missing
                Endpoint = "https://test.openai.azure.com",
                DeploymentName = "test-deployment"
            },
            Ollama = new OllamaSettings
            {
                Endpoint = "http://localhost:11434",
                ModelId = "llama2"
            }
        };
        var (service, _) = CreateService(settings);

        // Act
        var kernel = service.BuildKernel();

        // Assert
        Assert.Equal(AIServiceProvider.Ollama, service.CurrentProvider);
    }

    /// <summary>
    /// WHY: Verifies Azure is skipped when Endpoint is missing.
    /// </summary>
    [Fact]
    public void BuildKernel_WithAzureMissingEndpoint_FallsToOllama()
    {
        // Arrange
        var settings = new AIServiceSettings
        {
            AzureOpenAI = new AzureOpenAISettings
            {
                ApiKey = "test-key",
                Endpoint = "", // Missing
                DeploymentName = "test-deployment"
            },
            Ollama = new OllamaSettings
            {
                Endpoint = "http://localhost:11434",
                ModelId = "llama2"
            }
        };
        var (service, _) = CreateService(settings);

        // Act
        var kernel = service.BuildKernel();

        // Assert
        Assert.Equal(AIServiceProvider.Ollama, service.CurrentProvider);
    }

    /// <summary>
    /// WHY: Verifies Azure is skipped when DeploymentName is missing.
    /// </summary>
    [Fact]
    public void BuildKernel_WithAzureMissingDeploymentName_FallsToOllama()
    {
        // Arrange
        var settings = new AIServiceSettings
        {
            AzureOpenAI = new AzureOpenAISettings
            {
                ApiKey = "test-key",
                Endpoint = "https://test.openai.azure.com",
                DeploymentName = "" // Missing
            },
            Ollama = new OllamaSettings
            {
                Endpoint = "http://localhost:11434",
                ModelId = "llama2"
            }
        };
        var (service, _) = CreateService(settings);

        // Act
        var kernel = service.BuildKernel();

        // Assert
        Assert.Equal(AIServiceProvider.Ollama, service.CurrentProvider);
    }

    #endregion

    #region BuildKernel - Ollama Fallback Tests

    /// <summary>
    /// WHY: Verifies Ollama is selected when it's the only provider configured.
    /// </summary>
    [Fact]
    public void BuildKernel_WithOnlyOllama_SelectsOllama()
    {
        // Arrange
        var (service, _) = CreateService(CreateSettingsWithOllama());

        // Act
        var kernel = service.BuildKernel();

        // Assert
        Assert.NotNull(kernel);
        Assert.Equal(AIServiceProvider.Ollama, service.CurrentProvider);
    }

    /// <summary>
    /// WHY: Verifies Ollama is skipped when Endpoint is missing.
    /// </summary>
    [Fact]
    public void BuildKernel_WithOllamaMissingEndpoint_ThrowsException()
    {
        // Arrange
        var settings = new AIServiceSettings
        {
            Ollama = new OllamaSettings
            {
                Endpoint = "", // Missing
                ModelId = "llama2"
            }
        };
        var (service, _) = CreateService(settings);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => service.BuildKernel());
    }

    /// <summary>
    /// WHY: Verifies Ollama is skipped when ModelId is missing.
    /// </summary>
    [Fact]
    public void BuildKernel_WithOllamaMissingModelId_ThrowsException()
    {
        // Arrange
        var settings = new AIServiceSettings
        {
            Ollama = new OllamaSettings
            {
                Endpoint = "http://localhost:11434",
                ModelId = "" // Missing
            }
        };
        var (service, _) = CreateService(settings);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => service.BuildKernel());
    }

    #endregion

    #region BuildKernel - No Provider Configured Tests

    /// <summary>
    /// WHY: Verifies error when no providers are configured.
    /// </summary>
    [Fact]
    public void BuildKernel_WithNoProviders_ThrowsInvalidOperationException()
    {
        // Arrange
        var settings = new AIServiceSettings();
        var (service, _) = CreateService(settings);

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => service.BuildKernel());
        Assert.Contains("No AI service is properly configured", exception.Message);
    }

    /// <summary>
    /// WHY: Verifies error message mentions all providers.
    /// </summary>
    [Fact]
    public void BuildKernel_WithNoProviders_ErrorMentionsAllProviders()
    {
        // Arrange
        var settings = new AIServiceSettings();
        var (service, _) = CreateService(settings);

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => service.BuildKernel());
        Assert.Contains("OpenAI", exception.Message);
        Assert.Contains("Azure OpenAI", exception.Message);
        Assert.Contains("Ollama", exception.Message);
    }

    /// <summary>
    /// WHY: Verifies error when all providers have incomplete configs.
    /// </summary>
    [Fact]
    public void BuildKernel_WithAllProvidersIncomplete_ThrowsInvalidOperationException()
    {
        // Arrange
        var settings = new AIServiceSettings
        {
            OpenAI = new OpenAISettings { ApiKey = "" }, // Empty key
            AzureOpenAI = new AzureOpenAISettings { ApiKey = "" }, // Empty key
            Ollama = new OllamaSettings { Endpoint = "", ModelId = "" } // Empty
        };
        var (service, _) = CreateService(settings);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => service.BuildKernel());
    }

    #endregion

    #region CurrentProvider Property Tests

    /// <summary>
    /// WHY: Verifies CurrentProvider is default before BuildKernel is called.
    /// </summary>
    [Fact]
    public void CurrentProvider_BeforeBuildKernel_HasDefaultValue()
    {
        // Arrange
        var (service, _) = CreateService(CreateSettingsWithOpenAI());

        // Assert - Default enum value (0)
        Assert.Equal(default(AIServiceProvider), service.CurrentProvider);
    }

    /// <summary>
    /// WHY: Verifies CurrentProvider is set after BuildKernel is called.
    /// </summary>
    [Fact]
    public void CurrentProvider_AfterBuildKernel_IsSet()
    {
        // Arrange
        var (service, _) = CreateService(CreateSettingsWithOpenAI());

        // Act
        service.BuildKernel();

        // Assert
        Assert.Equal(AIServiceProvider.OpenAI, service.CurrentProvider);
    }

    /// <summary>
    /// WHY: Verifies CurrentProvider changes if BuildKernel is called again.
    /// Note: This tests the behavior, though typically BuildKernel would be called once.
    /// </summary>
    [Fact]
    public void CurrentProvider_AfterMultipleBuildKernelCalls_ReflectsLastCall()
    {
        // Arrange - Use settings that will select OpenAI
        var (service, _) = CreateService(CreateSettingsWithOpenAI());

        // Act
        service.BuildKernel();
        var firstProvider = service.CurrentProvider;
        service.BuildKernel(); // Call again
        var secondProvider = service.CurrentProvider;

        // Assert - Should be consistent
        Assert.Equal(firstProvider, secondProvider);
        Assert.Equal(AIServiceProvider.OpenAI, service.CurrentProvider);
    }

    #endregion

    #region OpenAI Partial Configuration Tests

    /// <summary>
    /// WHY: Verifies OpenAI is skipped when ApiKey is null.
    /// </summary>
    [Fact]
    public void BuildKernel_WithOpenAINullApiKey_FallsToNextProvider()
    {
        // Arrange
        var settings = new AIServiceSettings
        {
            OpenAI = new OpenAISettings { ApiKey = null! },
            AzureOpenAI = new AzureOpenAISettings
            {
                ApiKey = "test-azure-key",
                Endpoint = "https://test.openai.azure.com",
                DeploymentName = "test-deployment"
            }
        };
        var (service, _) = CreateService(settings);

        // Act
        var kernel = service.BuildKernel();

        // Assert
        Assert.Equal(AIServiceProvider.AzureOpenAI, service.CurrentProvider);
    }

    /// <summary>
    /// WHY: Verifies OpenAI is skipped when settings object is null.
    /// </summary>
    [Fact]
    public void BuildKernel_WithOpenAISettingsNull_FallsToNextProvider()
    {
        // Arrange
        var settings = new AIServiceSettings
        {
            OpenAI = null,
            AzureOpenAI = new AzureOpenAISettings
            {
                ApiKey = "test-azure-key",
                Endpoint = "https://test.openai.azure.com",
                DeploymentName = "test-deployment"
            }
        };
        var (service, _) = CreateService(settings);

        // Act
        var kernel = service.BuildKernel();

        // Assert
        Assert.Equal(AIServiceProvider.AzureOpenAI, service.CurrentProvider);
    }

    #endregion
}
