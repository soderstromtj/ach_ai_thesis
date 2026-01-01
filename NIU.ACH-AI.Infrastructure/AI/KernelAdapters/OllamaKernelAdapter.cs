using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using NIU.ACH_AI.Application.Configuration;
using NIU.ACH_AI.Infrastructure.Configuration;

namespace NIU.ACH_AI.Infrastructure.AI.KernelAdapters
{
    /// <summary>
    /// Adapter implementation for building Ollama (local LLM) Kernel instances.
    /// </summary>
    public class OllamaKernelAdapter : BaseKernelAdapter
    {
        private readonly OllamaSettings _settings;

        /// <inheritdoc />
        public override AIServiceProvider SupportedProvider => AIServiceProvider.Ollama;

        /// <summary>
        /// Initializes a new instance of the <see cref="OllamaKernelAdapter"/> class.
        /// </summary>
        /// <param name="settings">Specific Ollama settings.</param>
        /// <param name="aiServiceSettings">Global AI service settings.</param>
        /// <param name="loggerFactory">Logger factory.</param>
        /// <param name="httpClientFactory">Factory for creating HttpClient instances.</param>
        public OllamaKernelAdapter(
            OllamaSettings settings,
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

            builder.AddOllamaChatCompletion(
                modelId: modelId,
                endpoint: new Uri(_settings.Endpoint),
                serviceId: "ollama"
            );

            RegisterLogger(builder);

            return builder.Build();
        }
    }
}
