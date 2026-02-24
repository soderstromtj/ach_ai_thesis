using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using NIU.ACH_AI.Application.Configuration;
using NIU.ACH_AI.Infrastructure.Configuration;
using System.Net.Http;

namespace NIU.ACH_AI.Infrastructure.AI.KernelAdapters
{
    /// <summary>
    /// Builds Semantic Kernel instances configured for Azure OpenAI.
    /// </summary>
    public class AzureOpenAIKernelAdapter : BaseKernelAdapter
    {
        private readonly AzureOpenAISettings _settings;

        /// <inheritdoc />
        public override AIServiceProvider SupportedProvider => AIServiceProvider.AzureOpenAI;

        /// <summary>
        /// Sets up the Azure OpenAI kernel adapter.
        /// </summary>
        /// <param name="settings">Specific Azure OpenAI settings.</param>
        /// <param name="aiServiceSettings">Global AI service settings.</param>
        /// <param name="loggerFactory">Logger factory.</param>
        /// <param name="httpClientFactory">Factory for creating HttpClient instances.</param>
        public AzureOpenAIKernelAdapter(
            AzureOpenAISettings settings,
            AIServiceSettings aiServiceSettings,
            ILoggerFactory loggerFactory,
            IHttpClientFactory httpClientFactory)
            : base(aiServiceSettings, loggerFactory, httpClientFactory)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }

        /// <inheritdoc />
        public override Kernel BuildKernel(string? modelIdOverride = null)
        {
            var builder = Kernel.CreateBuilder();

            // Use override if provided, otherwise use settings default
            var modelId = modelIdOverride ?? _settings.ModelId ?? string.Empty;

            // Create custom HttpClient with extended timeout for large payloads
            var httpClient = CreateHttpClient();

            builder.AddAzureOpenAIChatCompletion(
                deploymentName: _settings.DeploymentName,
                apiKey: _settings.ApiKey,
                endpoint: _settings.Endpoint,
                modelId: modelId,
                serviceId: "azure",
                httpClient: httpClient
            );

            RegisterLogger(builder);

            return builder.Build();
        }
    }
}
