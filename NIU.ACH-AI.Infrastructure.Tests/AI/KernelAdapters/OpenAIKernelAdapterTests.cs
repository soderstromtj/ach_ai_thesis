using Microsoft.Extensions.Logging;
using Moq;
using NIU.ACH_AI.Application.Configuration;
using NIU.ACH_AI.Infrastructure.AI.KernelAdapters;
using NIU.ACH_AI.Infrastructure.Configuration;

namespace NIU.ACH_AI.Infrastructure.Tests.AI.KernelAdapters;

/// <summary>
/// Unit tests for OpenAIKernelAdapter.
///
/// Testing Strategy:
/// -----------------
/// OpenAIKernelAdapter builds Semantic Kernel instances configured for OpenAI.
///
/// Key testing areas:
/// 1. Constructor - Null validation, proper initialization
/// 2. SupportedProvider - Returns correct provider type
/// 3. BuildKernel - Kernel creation with default and override model IDs
/// </summary>
public class OpenAIKernelAdapterTests
{
    #region Test Infrastructure

    private static (OpenAIKernelAdapter Adapter, Mock<ILoggerFactory> LoggerFactoryMock, Mock<IHttpClientFactory> HttpClientFactoryMock) CreateAdapter(
        OpenAISettings? openAISettings = null,
        AIServiceSettings? aiServiceSettings = null)
    {
        var loggerFactoryMock = new Mock<ILoggerFactory>();
        loggerFactoryMock
            .Setup(f => f.CreateLogger(It.IsAny<string>()))
            .Returns(Mock.Of<ILogger>());

        var httpClientFactoryMock = new Mock<IHttpClientFactory>();
        httpClientFactoryMock
            .Setup(f => f.CreateClient(It.IsAny<string>()))
            .Returns(new HttpClient());

        var settings = openAISettings ?? new OpenAISettings
        {
            ApiKey = "test-api-key",
            ModelId = "gpt-4o"
        };

        var serviceSettings = aiServiceSettings ?? new AIServiceSettings
        {
            HttpTimeoutSeconds = 300
        };

        var adapter = new OpenAIKernelAdapter(settings, serviceSettings, loggerFactoryMock.Object, httpClientFactoryMock.Object);

        return (adapter, loggerFactoryMock, httpClientFactoryMock);
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
        var (adapter, _, _) = CreateAdapter();

        // Assert
        Assert.NotNull(adapter);
    }

    /// <summary>
    /// WHY: Verifies null OpenAISettings throws ArgumentNullException.
    /// </summary>
    [Fact]
    public void Constructor_WithNullOpenAISettings_ThrowsArgumentNullException()
    {
        // Arrange
        var loggerFactoryMock = new Mock<ILoggerFactory>();
        var aiServiceSettings = new AIServiceSettings();

        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() =>
            new OpenAIKernelAdapter(null!, aiServiceSettings, loggerFactoryMock.Object, Mock.Of<IHttpClientFactory>()));
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
        var openAISettings = new OpenAISettings { ApiKey = "test" };

        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() =>
            new OpenAIKernelAdapter(openAISettings, null!, loggerFactoryMock.Object, Mock.Of<IHttpClientFactory>()));
        Assert.Equal("aiServiceSettings", exception.ParamName);
    }

    /// <summary>
    /// WHY: Verifies adapter does not accept a null loggerFactory.
    /// </summary>
    [Fact]
    public void Constructor_WithNullLoggerFactory_DoesThrow()
    {
        // Arrange
        var openAISettings = new OpenAISettings { ApiKey = "test" };
        var aiServiceSettings = new AIServiceSettings();

        // Act
        var exception = Record.Exception(() =>
            new OpenAIKernelAdapter(openAISettings, aiServiceSettings, null!, Mock.Of<IHttpClientFactory>()));

        // Assert - LoggerFactory not optional
        Assert.NotNull(exception);
    }

    #endregion

    #region SupportedProvider Tests

    /// <summary>
    /// WHY: Verifies SupportedProvider returns OpenAI.
    /// </summary>
    [Fact]
    public void SupportedProvider_Always_ReturnsOpenAI()
    {
        // Arrange
        var (adapter, _, _) = CreateAdapter();

        // Act
        var provider = adapter.SupportedProvider;

        // Assert
        Assert.Equal(AIServiceProvider.OpenAI, provider);
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
        var (adapter, _, _) = CreateAdapter();

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
        var (adapter, _, _) = CreateAdapter();

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
        var (adapter, _, _) = CreateAdapter();

        // Act
        var kernel = adapter.BuildKernel("gpt-3.5-turbo");

        // Assert
        Assert.NotNull(kernel);
    }

    /// <summary>
    /// WHY: Verifies BuildKernel works with different model IDs.
    /// </summary>
    [Theory]
    [InlineData("gpt-4")]
    [InlineData("gpt-4o")]
    [InlineData("gpt-4o-mini")]
    [InlineData("gpt-3.5-turbo")]
    public void BuildKernel_WithDifferentModelIds_ReturnsKernel(string modelId)
    {
        // Arrange
        var (adapter, _, _) = CreateAdapter();

        // Act
        var kernel = adapter.BuildKernel(modelId);

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
        var (adapter, _, _) = CreateAdapter(aiServiceSettings: settings);

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
        var (adapter, _, _) = CreateAdapter();

        // Assert
        Assert.IsAssignableFrom<IKernelBuilderAdapter>(adapter);
    }

    #endregion

    #region Settings Tests

    /// <summary>
    /// WHY: Verifies adapter works with OrganizationId.
    /// </summary>
    [Fact]
    public void BuildKernel_WithOrganizationId_ReturnsKernel()
    {
        // Arrange
        var settings = new OpenAISettings
        {
            ApiKey = "test-key",
            ModelId = "gpt-4o",
            OrganizationId = "org-123456"
        };
        var (adapter, _, _) = CreateAdapter(openAISettings: settings);

        // Act
        var kernel = adapter.BuildKernel();

        // Assert
        Assert.NotNull(kernel);
    }

    /// <summary>
    /// WHY: Verifies adapter works without OrganizationId (null).
    /// </summary>
    [Fact]
    public void BuildKernel_WithNullOrganizationId_ReturnsKernel()
    {
        // Arrange
        var settings = new OpenAISettings
        {
            ApiKey = "test-key",
            ModelId = "gpt-4o",
            OrganizationId = null
        };
        var (adapter, _, _) = CreateAdapter(openAISettings: settings);

        // Act
        var kernel = adapter.BuildKernel();

        // Assert
        Assert.NotNull(kernel);
    }

    #endregion
}
