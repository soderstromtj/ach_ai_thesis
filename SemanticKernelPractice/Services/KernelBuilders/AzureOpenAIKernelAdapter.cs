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

        public AzureOpenAIKernelAdapter(ExperimentConfiguration experimentConfig, ILoggerFactory loggerFactory)
        {
            _settings = experimentConfig.GlobalAIServiceSettings.AzureOpenAI
                ?? throw new InvalidOperationException("AzureOpenAI settings not configured in AIServiceSettings");
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
                serviceId: _settings.ServiceId ?? string.Empty

                );

            builder.Services.AddSingleton(_loggerFactory);

            return builder.Build();
        }
    }
}