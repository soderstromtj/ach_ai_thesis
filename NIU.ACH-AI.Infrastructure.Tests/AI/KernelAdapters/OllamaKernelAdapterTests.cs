using Microsoft.Extensions.Logging;
using Moq;
using NIU.ACH_AI.Application.Configuration;
using NIU.ACH_AI.Infrastructure.AI.KernelAdapters;
using NIU.ACH_AI.Infrastructure.Configuration;

namespace NIU.ACH_AI.Infrastructure.Tests.AI.KernelAdapters;

/// <summary>
/// Unit tests for OllamaKernelAdapter.
///
/// Testing Strategy:
/// -----------------
/// OllamaKernelAdapter builds Semantic Kernel instances configured for Ollama.
///
/// Key testing areas:
/// 1. Constructor - Null validation, proper initialization
/// 2. SupportedProvider - Returns correct provider type
/// 3. BuildKernel - Kernel creation with default and override model IDs
/// 4. Endpoint handling - URI parsing and validation
/// </summary>
public class OllamaKernelAdapterTests
{
    #region Test Infrastructure

    private static (OllamaKernelAdapter Adapter, Mock<ILoggerFactory> LoggerFactoryMock) CreateAdapter(
        OllamaSettings? ollamaSettings = null,
        AIServiceSettings? aiServiceSettings = null)
    {
        var loggerFactoryMock = new Mock<ILoggerFactory>();
        loggerFactoryMock
            .Setup(f => f.CreateLogger(It.IsAny<string>()))
            .Returns(Mock.Of<ILogger>());

        var settings = ollamaSettings ?? new OllamaSettings
        {
            Endpoint = "http://localhost:11434",
            ModelId = "llama2"
        };

        var serviceSettings = aiServiceSettings ?? new AIServiceSettings
        {
            HttpTimeoutSeconds = 300
        };

        var adapter = new OllamaKernelAdapter(settings, serviceSettings, loggerFactoryMock.Object);

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
    /// WHY: Verifies null OllamaSettings throws ArgumentNullException.
    /// </summary>
    [Fact]
    public void Constructor_WithNullOllamaSettings_ThrowsArgumentNullException()
    {
        // Arrange
        var loggerFactoryMock = new Mock<ILoggerFactory>();
        var aiServiceSettings = new AIServiceSettings();

        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() =>
            new OllamaKernelAdapter(null!, aiServiceSettings, loggerFactoryMock.Object));
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
        var ollamaSettings = new OllamaSettings
        {
            Endpoint = "http://localhost:11434",
            ModelId = "llama2"
        };

        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() =>
            new OllamaKernelAdapter(ollamaSettings, null!, loggerFactoryMock.Object));
        Assert.Equal("aiServiceSettings", exception.ParamName);
    }

    /// <summary>
    /// WHY: Verifies adapter accepts null loggerFactory.
    /// </summary>
    [Fact]
    public void Constructor_WithNullLoggerFactory_DoesNotThrow()
    {
        // Arrange
        var ollamaSettings = new OllamaSettings
        {
            Endpoint = "http://localhost:11434",
            ModelId = "llama2"
        };
        var aiServiceSettings = new AIServiceSettings();

        // Act
        var exception = Record.Exception(() =>
            new OllamaKernelAdapter(ollamaSettings, aiServiceSettings, null!));

        // Assert
        Assert.Null(exception);
    }

    #endregion

    #region SupportedProvider Tests

    /// <summary>
    /// WHY: Verifies SupportedProvider returns Ollama.
    /// </summary>
    [Fact]
    public void SupportedProvider_Always_ReturnsOllama()
    {
        // Arrange
        var (adapter, _) = CreateAdapter();

        // Act
        var provider = adapter.SupportedProvider;

        // Assert
        Assert.Equal(AIServiceProvider.Ollama, provider);
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
        var kernel = adapter.BuildKernel("mistral");

        // Assert
        Assert.NotNull(kernel);
    }

    /// <summary>
    /// WHY: Verifies BuildKernel works with different model IDs.
    /// </summary>
    [Theory]
    [InlineData("llama2")]
    [InlineData("llama3")]
    [InlineData("mistral")]
    [InlineData("codellama")]
    [InlineData("phi")]
    public void BuildKernel_WithDifferentModelIds_ReturnsKernel(string modelId)
    {
        // Arrange
        var (adapter, _) = CreateAdapter();

        // Act
        var kernel = adapter.BuildKernel(modelId);

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

    #region Endpoint Tests

    /// <summary>
    /// WHY: Verifies adapter works with different endpoint formats.
    /// </summary>
    [Theory]
    [InlineData("http://localhost:11434")]
    [InlineData("http://127.0.0.1:11434")]
    [InlineData("http://ollama:11434")]
    [InlineData("http://192.168.1.100:11434")]
    public void BuildKernel_WithDifferentEndpoints_ReturnsKernel(string endpoint)
    {
        // Arrange
        var settings = new OllamaSettings
        {
            Endpoint = endpoint,
            ModelId = "llama2"
        };
        var (adapter, _) = CreateAdapter(ollamaSettings: settings);

        // Act
        var kernel = adapter.BuildKernel();

        // Assert
        Assert.NotNull(kernel);
    }

    /// <summary>
    /// WHY: Verifies adapter works with custom ports.
    /// </summary>
    [Theory]
    [InlineData("http://localhost:8080")]
    [InlineData("http://localhost:3000")]
    [InlineData("http://localhost:5000")]
    public void BuildKernel_WithCustomPorts_ReturnsKernel(string endpoint)
    {
        // Arrange
        var settings = new OllamaSettings
        {
            Endpoint = endpoint,
            ModelId = "llama2"
        };
        var (adapter, _) = CreateAdapter(ollamaSettings: settings);

        // Act
        var kernel = adapter.BuildKernel();

        // Assert
        Assert.NotNull(kernel);
    }

    /// <summary>
    /// WHY: Verifies invalid endpoint throws UriFormatException.
    /// </summary>
    [Fact]
    public void BuildKernel_WithInvalidEndpoint_ThrowsUriFormatException()
    {
        // Arrange
        var settings = new OllamaSettings
        {
            Endpoint = "not-a-valid-uri",
            ModelId = "llama2"
        };
        var (adapter, _) = CreateAdapter(ollamaSettings: settings);

        // Act & Assert
        Assert.Throws<UriFormatException>(() => adapter.BuildKernel());
    }

    /// <summary>
    /// WHY: Verifies empty endpoint throws UriFormatException.
    /// </summary>
    [Fact]
    public void BuildKernel_WithEmptyEndpoint_ThrowsUriFormatException()
    {
        // Arrange
        var settings = new OllamaSettings
        {
            Endpoint = "",
            ModelId = "llama2"
        };
        var (adapter, _) = CreateAdapter(ollamaSettings: settings);

        // Act & Assert
        Assert.Throws<UriFormatException>(() => adapter.BuildKernel());
    }

    #endregion

    #region Default Settings Tests

    /// <summary>
    /// WHY: Verifies default OllamaSettings has proper defaults.
    /// </summary>
    [Fact]
    public void OllamaSettings_HasDefaultValues()
    {
        // Arrange
        var settings = new OllamaSettings();

        // Assert
        Assert.Equal("http://localhost:11434", settings.Endpoint);
        Assert.Equal("llama2", settings.ModelId);
    }

    /// <summary>
    /// WHY: Verifies adapter works with default settings.
    /// </summary>
    [Fact]
    public void BuildKernel_WithDefaultSettings_ReturnsKernel()
    {
        // Arrange
        var settings = new OllamaSettings(); // Uses defaults
        var (adapter, _) = CreateAdapter(ollamaSettings: settings);

        // Act
        var kernel = adapter.BuildKernel();

        // Assert
        Assert.NotNull(kernel);
    }

    #endregion
}
