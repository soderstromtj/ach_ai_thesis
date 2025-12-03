using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using SemanticKernelPractice.Configuration;

namespace SemanticKernelPractice.Services.KernelBuilders
{
    public class HuggingFaceKernelAdapter : IKernelBuilderAdapter
    {
        private readonly HuggingFaceSettings _settings;
        private readonly ILoggerFactory _loggerFactory;

        public AIServiceProvider SupportedProvider => AIServiceProvider.HuggingFace;

        public HuggingFaceKernelAdapter(
            ExperimentConfiguration experimentConfig,
            ILoggerFactory loggerFactory)
        {
            _settings = experimentConfig.GlobalAIServiceSettings.HuggingFace
                ?? throw new InvalidOperationException("HuggingFace settings not configured in AIServiceSettings");
            _loggerFactory = loggerFactory;
        }

        public Kernel BuildKernel()
        {
            var builder = Kernel.CreateBuilder();

            builder.AddHuggingFaceChatCompletion(
                model: _settings.ModelId,
                apiKey: _settings.ApiKey);

            builder.Services.AddSingleton(_loggerFactory);

            return builder.Build();
        }
    }
}
