using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using NIU.ACH_AI.Application.Configuration;
using NIU.ACH_AI.Infrastructure.Configuration;

namespace NIU.ACH_AI.Infrastructure.AI.KernelAdapters
{
    /// <summary>
    /// Adapter implementation for building OpenAI Kernel instances.
    /// </summary>
    public class OpenAIKernelAdapter : BaseKernelAdapter
    {
        private readonly OpenAISettings _settings;

        /// <inheritdoc />
        public override AIServiceProvider SupportedProvider => AIServiceProvider.OpenAI;

        /// <summary>
        /// Initializes a new instance of the <see cref="OpenAIKernelAdapter"/> class.
        /// </summary>
        /// <param name="settings">Specific OpenAI settings.</param>
        /// <param name="aiServiceSettings">Global AI service settings.</param>
        /// <param name="loggerFactory">Logger factory.</param>
        /// <param name="httpClientFactory">Factory for creating HttpClient instances.</param>
        public OpenAIKernelAdapter(
            OpenAISettings settings,
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
            var modelId = modelIdOverride ?? _settings.ModelId;

            // Create custom HttpClient with extended timeout for large payloads
            var httpClient = CreateHttpClient();

            builder.AddOpenAIChatCompletion(
                modelId: modelId,
                apiKey: _settings.ApiKey,
                orgId: _settings.OrganizationId ?? string.Empty,
                serviceId: "openai",
                httpClient: httpClient
            );

            RegisterLogger(builder);

            return builder.Build();
        }
    }
}
