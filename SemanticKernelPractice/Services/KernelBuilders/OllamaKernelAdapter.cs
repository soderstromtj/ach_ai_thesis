using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using SemanticKernelPractice.Configuration;

namespace SemanticKernelPractice.Services.KernelBuilders
{
    public class OllamaKernelAdapter : IKernelBuilderAdapter
    {
        private readonly OllamaSettings _settings;
        private readonly AIServiceSettings _aiServiceSettings;
        private readonly ILoggerFactory _loggerFactory;

        public AIServiceProvider SupportedProvider => AIServiceProvider.Ollama;

        public OllamaKernelAdapter(
            OllamaSettings settings,
            AIServiceSettings aiServiceSettings,
            ILoggerFactory loggerFactory)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _aiServiceSettings = aiServiceSettings ?? throw new ArgumentNullException(nameof(aiServiceSettings));
            _loggerFactory = loggerFactory;
        }

        public Kernel BuildKernel()
        {
            var builder = Kernel.CreateBuilder();

            // Create custom HttpClient with extended timeout for large payloads
            var httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(_aiServiceSettings.HttpTimeoutSeconds)
            };

            builder.AddOllamaChatCompletion(
                modelId: _settings.ModelId,
                endpoint: new Uri(_settings.Endpoint),
                serviceId: "ollama",
                httpClient: httpClient
            );

            builder.Services.AddSingleton(_loggerFactory);

            return builder.Build();
        }
    }
}
