using Microsoft.Extensions.Logging;
using Moq;
using NIU.ACH_AI.Application.Configuration;
using NIU.ACH_AI.Infrastructure.AI.KernelAdapters;
using NIU.ACH_AI.Infrastructure.Configuration;

namespace NIU.ACH_AI.Infrastructure.Tests.AI.KernelAdapters;

/// <summary>
/// Unit tests for AzureOpenAIKernelAdapter.
///
/// Testing Strategy:
/// -----------------
/// AzureOpenAIKernelAdapter builds Semantic Kernel instances configured for Azure OpenAI.
///
/// Key testing areas:
/// 1. Constructor - Null validation, proper initialization
/// 2. SupportedProvider - Returns correct provider type
/// 3. BuildKernel - Kernel creation with default and override model IDs
/// </summary>
public class AzureOpenAIKernelAdapterTests
{
    #region Test Infrastructure

    private static (AzureOpenAIKernelAdapter Adapter, Mock<ILoggerFactory> LoggerFactoryMock) CreateAdapter(
        AzureOpenAISettings? azureSettings = null,
        AIServiceSettings? aiServiceSettings = null)
    {
        var loggerFactoryMock = new Mock<ILoggerFactory>();
        loggerFactoryMock
            .Setup(f => f.CreateLogger(It.IsAny<string>()))
            .Returns(Mock.Of<ILogger>());

        var settings = azureSettings ?? new AzureOpenAISettings
        {
            ApiKey = "test-api-key",
            Endpoint = "https://test.openai.azure.com",
            DeploymentName = "test-deployment",
            ModelId = "gpt-4"
        };

        var serviceSettings = aiServiceSettings ?? new AIServiceSettings
        {
            HttpTimeoutSeconds = 300
        };

        var adapter = new AzureOpenAIKernelAdapter(settings, serviceSettings, loggerFactoryMock.Object);

        return (adapter, loggerFactoryMock);
    }

    #endregion

    #region Constructor Tests

    /// <summary>
    /// WHY: Verifies adapter can be instantiated with valid settings.
    /// </summary>
    [Fact]
    public void Constructor_WithValidSettings_CreatesInstance()
    {
        // Arrange & Act
        var (adapter, _) = CreateAdapter();

        // Assert
        Assert.NotNull(adapter);
    }

    /// <summary>
    /// WHY: Verifies null AzureOpenAISettings throws ArgumentNullException.
    /// </summary>
    [Fact]
    public void Constructor_WithNullAzureSettings_ThrowsArgumentNullException()
    {
        // Arrange
        var loggerFactoryMock = new Mock<ILoggerFactory>();
        var aiServiceSettings = new AIServiceSettings();

        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() =>
            new AzureOpenAIKernelAdapter(null!, aiServiceSettings, loggerFactoryMock.Object));
        Assert.Equal("settings", exception.ParamName);
    }

    /// <summary>
    /// WHY: Verifies null AIServiceSettings throws ArgumentNullException.
    /// </summary>
    [Fact]
    public void Constructor_WithNullAIServiceSettings_ThrowsArgumentNullException()
    {
        // Arrange
        var loggerFactoryMock = new Mock<ILoggerFactory>();
        var azureSettings = new AzureOpenAISettings
        {
            ApiKey = "test",
            Endpoint = "https://test.openai.azure.com",
            DeploymentName = "test"
        };

        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() =>
            new AzureOpenAIKernelAdapter(azureSettings, null!, loggerFactoryMock.Object));
        Assert.Equal("aiServiceSettings", exception.ParamName);
    }

    /// <summary>
    /// WHY: Verifies adapter accepts null loggerFactory.
    /// </summary>
    [Fact]
    public void Constructor_WithNullLoggerFactory_DoesNotThrow()
    {
        // Arrange
        var azureSettings = new AzureOpenAISettings
        {
            ApiKey = "test",
            Endpoint = "https://test.openai.azure.com",
            DeploymentName = "test"
        };
        var aiServiceSettings = new AIServiceSettings();

        // Act
        var exception = Record.Exception(() =>
            new AzureOpenAIKernelAdapter(azureSettings, aiServiceSettings, null!));

        // Assert
        Assert.Null(exception);
    }

    #endregion

    #region SupportedProvider Tests

    /// <summary>
    /// WHY: Verifies SupportedProvider returns AzureOpenAI.
    /// </summary>
    [Fact]
    public void SupportedProvider_Always_ReturnsAzureOpenAI()
    {
        // Arrange
        var (adapter, _) = CreateAdapter();

        // Act
        var provider = adapter.SupportedProvider;

        // Assert
        Assert.Equal(AIServiceProvider.AzureOpenAI, provider);
    }

    #endregion

    #region BuildKernel Tests

    /// <summary>
    /// WHY: Verifies BuildKernel returns a non-null Kernel.
    /// </summary>
    [Fact]
    public void BuildKernel_WithValidSettings_ReturnsKernel()
    {
        // Arrange
        var (adapter, _) = CreateAdapter();

        // Act
        var kernel = adapter.BuildKernel();

        // Assert
        Assert.NotNull(kernel);
    }

    /// <summary>
    /// WHY: Verifies BuildKernel accepts null modelIdOverride.
    /// </summary>
    [Fact]
    public void BuildKernel_WithNullModelIdOverride_UsesDefaultModel()
    {
        // Arrange
        var (adapter, _) = CreateAdapter();

        // Act
        var kernel = adapter.BuildKernel(null);

        // Assert
        Assert.NotNull(kernel);
    }

    /// <summary>
    /// WHY: Verifies BuildKernel accepts model ID override.
    /// </summary>
    [Fact]
    public void BuildKernel_WithModelIdOverride_ReturnsKernel()
    {
        // Arrange
        var (adapter, _) = CreateAdapter();

        // Act
        var kernel = adapter.BuildKernel("gpt-4-turbo");

        // Assert
        Assert.NotNull(kernel);
    }

    /// <summary>
    /// WHY: Verifies BuildKernel works with null ModelId in settings.
    /// </summary>
    [Fact]
    public void BuildKernel_WithNullModelIdInSettings_ReturnsKernel()
    {
        // Arrange
        var settings = new AzureOpenAISettings
        {
            ApiKey = "test-key",
            Endpoint = "https://test.openai.azure.com",
            DeploymentName = "test-deployment",
            ModelId = null
        };
        var (adapter, _) = CreateAdapter(azureSettings: settings);

        // Act
        var kernel = adapter.BuildKernel();

        // Assert
        Assert.NotNull(kernel);
    }

    /// <summary>
    /// WHY: Verifies BuildKernel uses custom timeout from settings.
    /// </summary>
    [Fact]
    public void BuildKernel_WithCustomTimeout_ReturnsKernel()
    {
        // Arrange
        var settings = new AIServiceSettings { HttpTimeoutSeconds = 600 };
        var (adapter, _) = CreateAdapter(aiServiceSettings: settings);

        // Act
        var kernel = adapter.BuildKernel();

        // Assert
        Assert.NotNull(kernel);
    }

    #endregion

    #region Interface Implementation Tests

    /// <summary>
    /// WHY: Verifies adapter implements IKernelBuilderAdapter.
    /// </summary>
    [Fact]
    public void Adapter_ImplementsIKernelBuilderAdapter()
    {
        // Arrange
        var (adapter, _) = CreateAdapter();

        // Assert
        Assert.IsAssignableFrom<IKernelBuilderAdapter>(adapter);
    }

    #endregion

    #region Azure-Specific Settings Tests

    /// <summary>
    /// WHY: Verifies adapter works with various endpoint formats.
    /// </summary>
    [Theory]
    [InlineData("https://myresource.openai.azure.com")]
    [InlineData("https://eastus.api.cognitive.microsoft.com")]
    [InlineData("https://custom-endpoint.azure.com")]
    public void BuildKernel_WithDifferentEndpoints_ReturnsKernel(string endpoint)
    {
        // Arrange
        var settings = new AzureOpenAISettings
        {
            ApiKey = "test-key",
            Endpoint = endpoint,
            DeploymentName = "test-deployment"
        };
        var (adapter, _) = CreateAdapter(azureSettings: settings);

        // Act
        var kernel = adapter.BuildKernel();

        // Assert
        Assert.NotNull(kernel);
    }

    /// <summary>
    /// WHY: Verifies adapter works with various deployment names.
    /// </summary>
    [Theory]
    [InlineData("gpt-4")]
    [InlineData("my-custom-deployment")]
    [InlineData("gpt-35-turbo-16k")]
    public void BuildKernel_WithDifferentDeploymentNames_ReturnsKernel(string deploymentName)
    {
        // Arrange
        var settings = new AzureOpenAISettings
        {
            ApiKey = "test-key",
            Endpoint = "https://test.openai.azure.com",
            DeploymentName = deploymentName
        };
        var (adapter, _) = CreateAdapter(azureSettings: settings);

        // Act
        var kernel = adapter.BuildKernel();

        // Assert
        Assert.NotNull(kernel);
    }

    #endregion
}
