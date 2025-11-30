using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using SemanticKernelPractice.Configuration;

namespace SemanticKernelPractice.Services.KernelBuilders
{
    public class OllamaKernelAdapter : IKernelBuilderAdapter
    {
        private readonly OllamaSettings _settings;
        private readonly ILoggerFactory _loggerFactory;

        public AIServiceProvider SupportedProvider => AIServiceProvider.Ollama;

        public OllamaKernelAdapter(
            IOptions<AIServiceSettings> serviceSettings,
            ILoggerFactory loggerFactory)
        {
            _settings = serviceSettings.Value.Ollama
                ?? throw new InvalidOperationException("Ollama settings not configured");
            _loggerFactory = loggerFactory;
        }

        public Kernel BuildKernel()
        {
            var builder = Kernel.CreateBuilder();

            builder.AddOllamaChatCompletion(
                modelId: _settings.ModelId,
                endpoint: new Uri(_settings.Endpoint));

            builder.Services.AddSingleton(_loggerFactory);

            return builder.Build();
        }
    }
}