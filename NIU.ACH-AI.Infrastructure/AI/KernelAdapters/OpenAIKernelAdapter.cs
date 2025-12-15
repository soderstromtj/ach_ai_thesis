using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using NIU.ACH_AI.Infrastructure.Configuration;

namespace NIU.ACH_AI.Infrastructure.AI.KernelAdapters
{
    public class OpenAIKernelAdapter : IKernelBuilderAdapter
    {
        private readonly OpenAISettings _settings;
        private readonly AIServiceSettings _aiServiceSettings;
        private readonly ILoggerFactory _loggerFactory;

        public AIServiceProvider SupportedProvider => AIServiceProvider.OpenAI;

        public OpenAIKernelAdapter(
            OpenAISettings settings,
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

            // Create custom HttpClient with extended timeout for large payloads
            var httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(_aiServiceSettings.HttpTimeoutSeconds)
            };

            builder.AddOpenAIChatCompletion(
                modelId: modelId,
                apiKey: _settings.ApiKey,
                orgId: _settings.OrganizationId ?? string.Empty,
                serviceId: "openai",
                httpClient: httpClient
            );

            builder.Services.AddSingleton(_loggerFactory);

            return builder.Build();
        }
    }
}
