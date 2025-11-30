using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using SemanticKernelPractice.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SemanticKernelPractice.Services.KernelBuilders
{
    public class HuggingFaceKernelAdapter : IKernelBuilderAdapter
    {
        private readonly HuggingFaceSettings _settings;
        private readonly ILoggerFactory _loggerFactory;

        public AIServiceProvider SupportedProvider => AIServiceProvider.HuggingFace;

        public HuggingFaceKernelAdapter(
            IOptions<AIServiceSettings> serviceSettings,
            ILoggerFactory loggerFactory)
        {
            _settings = serviceSettings.Value.HuggingFace
                ?? throw new InvalidOperationException("Microsoft Foundry settings not configured");
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
