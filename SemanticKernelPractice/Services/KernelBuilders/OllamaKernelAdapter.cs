using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
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
            OllamaSettings settings,
            ILoggerFactory loggerFactory)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _loggerFactory = loggerFactory;
        }

        public Kernel BuildKernel()
        {
            var builder = Kernel.CreateBuilder();

            builder.AddOllamaChatCompletion(
                modelId: _settings.ModelId,
                endpoint: new Uri(_settings.Endpoint),
                serviceId: "ollama"
            );

            builder.Services.AddSingleton(_loggerFactory);

            return builder.Build();
        }
    }
}
