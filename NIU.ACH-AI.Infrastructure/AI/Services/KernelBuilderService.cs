using System.Net.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using NIU.ACH_AI.Application.Configuration;
using NIU.ACH_AI.Application.Interfaces;
using NIU.ACH_AI.Infrastructure.AI.KernelAdapters;
using NIU.ACH_AI.Infrastructure.Configuration;

namespace NIU.ACH_AI.Infrastructure.AI.Services
{
    /// <summary>
    /// Builds Kernel instances and selects the appropriate provider.
    /// </summary>
    public class KernelBuilderService : IKernelBuilderService
    {
        private readonly AIServiceSettings _aiServiceSettings;
        private readonly ILoggerFactory _loggerFactory;
        private readonly IHttpClientFactory _httpClientFactory;

        /// <inheritdoc />
        public AIServiceProvider CurrentProvider { get; private set; }

        /// <summary>
        /// Sets up the kernel builder service.
        /// </summary>
        /// <param name="aiServiceSettings">The AI service settings options.</param>
        /// <param name="loggerFactory">The logger factory.</param>
        /// <param name="httpClientFactory">The http client factory.</param>
        public KernelBuilderService(
            IOptions<AIServiceSettings> aiServiceSettings,
            ILoggerFactory loggerFactory,
            IHttpClientFactory httpClientFactory)
        {
            _aiServiceSettings = aiServiceSettings.Value;
            _loggerFactory = loggerFactory;
            _httpClientFactory = httpClientFactory;
        }

        public Kernel BuildKernel()
        {
            // Build a default kernel for orchestration purposes (e.g., structured output transformation)
            // Try providers in order of preference: OpenAI, Azure OpenAI, Ollama

            if (_aiServiceSettings.OpenAI != null &&
                !string.IsNullOrWhiteSpace(_aiServiceSettings.OpenAI.ApiKey))
            {
                CurrentProvider = AIServiceProvider.OpenAI;
                var adapter = new OpenAIKernelAdapter(_aiServiceSettings.OpenAI, _aiServiceSettings, _loggerFactory, _httpClientFactory);
                return adapter.BuildKernel();
            }

            if (_aiServiceSettings.AzureOpenAI != null &&
                !string.IsNullOrWhiteSpace(_aiServiceSettings.AzureOpenAI.ApiKey) &&
                !string.IsNullOrWhiteSpace(_aiServiceSettings.AzureOpenAI.Endpoint) &&
                !string.IsNullOrWhiteSpace(_aiServiceSettings.AzureOpenAI.DeploymentName))
            {
                CurrentProvider = AIServiceProvider.AzureOpenAI;
                var adapter = new AzureOpenAIKernelAdapter(_aiServiceSettings.AzureOpenAI, _aiServiceSettings, _loggerFactory, _httpClientFactory);
                return adapter.BuildKernel();
            }

            if (_aiServiceSettings.Ollama != null &&
                !string.IsNullOrWhiteSpace(_aiServiceSettings.Ollama.Endpoint) &&
                !string.IsNullOrWhiteSpace(_aiServiceSettings.Ollama.ModelId))
            {
                CurrentProvider = AIServiceProvider.Ollama;
                var adapter = new OllamaKernelAdapter(_aiServiceSettings.Ollama, _aiServiceSettings, _loggerFactory, _httpClientFactory);
                return adapter.BuildKernel();
            }

            throw new InvalidOperationException(
                "No AI service is properly configured for orchestration. Please ensure at least one AI service " +
                "(OpenAI, Azure OpenAI, or Ollama) is configured in AIServiceSettings.");
        }
    }
}
