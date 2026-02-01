using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Moq;
using NIU.ACH_AI.Application.Configuration;
using NIU.ACH_AI.Infrastructure.AI.KernelAdapters;
using NIU.ACH_AI.Infrastructure.Configuration;

namespace NIU.ACH_AI.Infrastructure.Tests.AI.KernelAdapters;

public class BaseKernelAdapterTests
{
    private readonly Mock<ILoggerFactory> _mockLoggerFactory;
    private readonly Mock<IHttpClientFactory> _mockHttpClientFactory;
    private readonly AIServiceSettings _settings;

    public BaseKernelAdapterTests()
    {
        _mockLoggerFactory = new Mock<ILoggerFactory>();
        _mockHttpClientFactory = new Mock<IHttpClientFactory>();
        _settings = new AIServiceSettings 
        { 
            HttpTimeoutSeconds = 60 
        };
    }

    [Fact]
    public void CreateHttpClient_ReturnsClientWithConfiguredTimeout()
    {
        // Arrange
        var adapter = new TestKernelAdapter(_settings, _mockLoggerFactory.Object, _mockHttpClientFactory.Object);

        // Act
        var client = adapter.GetHttpClient();

        // Assert
        client.Should().NotBeNull();
        client.Timeout.Should().Be(TimeSpan.FromSeconds(60));
    }

    [Fact]
    public void Constructor_WithNullArguments_ThrowsArgumentNullException()
    {
        // Assert
        Assert.Throws<ArgumentNullException>(() => new TestKernelAdapter(null!, _mockLoggerFactory.Object, _mockHttpClientFactory.Object));
        Assert.Throws<ArgumentNullException>(() => new TestKernelAdapter(_settings, null!, _mockHttpClientFactory.Object));
        Assert.Throws<ArgumentNullException>(() => new TestKernelAdapter(_settings, _mockLoggerFactory.Object, null!));
    }

    // Test Harness
    private class TestKernelAdapter : BaseKernelAdapter
    {
        public TestKernelAdapter(
            AIServiceSettings aiServiceSettings,
            ILoggerFactory loggerFactory,
            IHttpClientFactory httpClientFactory) 
            : base(aiServiceSettings, loggerFactory, httpClientFactory)
        {
        }

        public override AIServiceProvider SupportedProvider => AIServiceProvider.OpenAI;

        public override Kernel BuildKernel(string? modelIdOverride = null)
        {
            throw new NotImplementedException();
        }

        // Expose protected method for testing
        public HttpClient GetHttpClient()
        {
            return CreateHttpClient();
        }
    }
}
