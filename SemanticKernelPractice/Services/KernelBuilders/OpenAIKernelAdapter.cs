using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using SemanticKernelPractice.Configuration;

namespace SemanticKernelPractice.Services.KernelBuilders
{
    public class OpenAIKernelAdapter : IKernelBuilderAdapter
    {
        private readonly OpenAISettings _settings;
        private readonly ILoggerFactory _loggerFactory;

        public AIServiceProvider SupportedProvider => AIServiceProvider.OpenAI;

        public OpenAIKernelAdapter(
            ExperimentConfiguration experimentConfig,
            ILoggerFactory loggerFactory)
        {
            _settings = experimentConfig.GlobalAIServiceSettings.OpenAI
                ?? throw new InvalidOperationException("OpenAI settings not configured in AIServiceSettings");
            _loggerFactory = loggerFactory;
        }

        public Kernel BuildKernel()
        {
            var builder = Kernel.CreateBuilder();

            builder.AddOpenAIChatCompletion(
                modelId: _settings.ModelId,
                apiKey: _settings.ApiKey,
                orgId: _settings.OrganizationId ?? string.Empty,
                serviceId: _settings.ServiceId ?? string.Empty
            );
            
            builder.Services.AddSingleton(_loggerFactory);

            return builder.Build();
        }
    }
}