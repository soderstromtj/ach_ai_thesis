using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using SemanticKernelPractice.Configuration;

namespace SemanticKernelPractice.Services.KernelBuilders
{
    public class AzureOpenAIKernelAdapter : IKernelBuilderAdapter
    {
        private readonly AzureOpenAISettings _settings;
        private readonly ILoggerFactory _loggerFactory;

        public AIServiceProvider SupportedProvider => AIServiceProvider.AzureOpenAI;

        public AzureOpenAIKernelAdapter(
            AzureOpenAISettings settings,
            ILoggerFactory loggerFactory)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _loggerFactory = loggerFactory;
        }

        public Kernel BuildKernel()
        {
            var builder = Kernel.CreateBuilder();

            builder.AddAzureOpenAIChatCompletion(
                deploymentName: _settings.DeploymentName,
                apiKey: _settings.ApiKey,
                endpoint: _settings.Endpoint,
                modelId: _settings.ModelId ?? string.Empty,
                serviceId: "azure"
            );

            builder.Services.AddSingleton(_loggerFactory);

            return builder.Build();
        }
    }
}
