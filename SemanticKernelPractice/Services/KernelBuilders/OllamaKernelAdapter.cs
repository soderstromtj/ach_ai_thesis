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

        public Kernel BuildKernel(string? modelIdOverride = null)
        {
            var builder = Kernel.CreateBuilder();

            // Use override if provided, otherwise use settings default
            var modelId = modelIdOverride ?? _settings.ModelId;

            builder.AddOllamaChatCompletion(
                modelId: modelId,
                endpoint: new Uri(_settings.Endpoint),
                serviceId: "ollama"
            );

            builder.Services.AddSingleton(_loggerFactory);

            return builder.Build();
        }
    }
}
