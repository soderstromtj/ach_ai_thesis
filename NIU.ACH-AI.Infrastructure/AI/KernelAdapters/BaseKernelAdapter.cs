using System.Net.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using NIU.ACH_AI.Application.Configuration;
using NIU.ACH_AI.Infrastructure.Configuration;

namespace NIU.ACH_AI.Infrastructure.AI.KernelAdapters
{
    /// <summary>
    /// Base class for Kernel Builder Adapters, providing common functionality.
    /// </summary>
    public abstract class BaseKernelAdapter : IKernelBuilderAdapter
    {
        protected readonly AIServiceSettings AiServiceSettings;
        protected readonly ILoggerFactory LoggerFactory;
        protected readonly IHttpClientFactory HttpClientFactory;

        /// <inheritdoc />
        public abstract AIServiceProvider SupportedProvider { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="BaseKernelAdapter"/> class.
        /// </summary>
        /// <param name="aiServiceSettings">Global AI service settings.</param>
        /// <param name="loggerFactory">Logger factory.</param>
        /// <param name="httpClientFactory">Factory for creating HttpClient instances.</param>
        protected BaseKernelAdapter(
            AIServiceSettings aiServiceSettings,
            ILoggerFactory loggerFactory,
            IHttpClientFactory httpClientFactory)
        {
            AiServiceSettings = aiServiceSettings ?? throw new ArgumentNullException(nameof(aiServiceSettings));
            LoggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
            HttpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        }

        /// <inheritdoc />
        public abstract Kernel BuildKernel(string? modelIdOverride = null);

        /// <summary>
        /// Creates an HttpClient with the configured timeout.
        /// </summary>
        /// <returns>A configured HttpClient.</returns>
        protected HttpClient CreateHttpClient()
        {
            // Bypass IHttpClientFactory to prevent global Polly policies (e.g. 30s timeout) from automatically attaching.
            // Create a pristine client for long-running AI requests.
            var handler = new SocketsHttpHandler
            {
                PooledConnectionLifetime = TimeSpan.FromMinutes(15)
            };
            
            var client = new HttpClient(handler);
            client.Timeout = TimeSpan.FromSeconds(AiServiceSettings.HttpTimeoutSeconds);
            return client;
        }

        /// <summary>
        /// Registers the logger factory with the kernel builder.
        /// </summary>
        /// <param name="builder">The kernel builder.</param>
        protected void RegisterLogger(IKernelBuilder builder)
        {
            builder.Services.AddSingleton(LoggerFactory);
        }
    }
}
