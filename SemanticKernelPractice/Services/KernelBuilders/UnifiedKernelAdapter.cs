using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using SemanticKernelPractice.Configuration;

namespace SemanticKernelPractice.Services.KernelBuilders
{
    /// <summary>
    /// Unified kernel adapter that registers all configured AI service providers into a single kernel.
    /// This allows for multi-provider scenarios such as fallback, A/B testing, and ensemble approaches.
    /// </summary>
    public class UnifiedKernelAdapter : IKernelBuilderAdapter
    {
        private readonly AIServiceSettings _aiServiceSettings;
        private readonly ILoggerFactory _loggerFactory;

        public AIServiceProvider SupportedProvider => AIServiceProvider.Unified;

        public UnifiedKernelAdapter(
            ExperimentConfiguration experimentConfig,
            ILoggerFactory loggerFactory)
        {
            _aiServiceSettings = experimentConfig.GlobalAIServiceSettings;
            _loggerFactory = loggerFactory;
        }

        public Kernel BuildKernel()
        {
            var builder = Kernel.CreateBuilder();
            var servicesAdded = 0;

            // Add Ollama if configured
            if (_aiServiceSettings.Ollama != null)
            {
                builder.AddOllamaChatCompletion(
                    modelId: _aiServiceSettings.Ollama.ModelId,
                    endpoint: new Uri(_aiServiceSettings.Ollama.Endpoint),
                    serviceId: "ollama");
                servicesAdded++;
            }

            // Add OpenAI if configured with API key
            if (_aiServiceSettings.OpenAI != null &&
                !string.IsNullOrWhiteSpace(_aiServiceSettings.OpenAI.ApiKey))
            {
                builder.AddOpenAIChatCompletion(
                    modelId: _aiServiceSettings.OpenAI.ModelId,
                    apiKey: _aiServiceSettings.OpenAI.ApiKey,
                    orgId: _aiServiceSettings.OpenAI.OrganizationId ?? string.Empty,
                    serviceId: "openai");
                servicesAdded++;
            }

            // Add Azure OpenAI if configured with required settings
            if (_aiServiceSettings.AzureOpenAI != null &&
                !string.IsNullOrWhiteSpace(_aiServiceSettings.AzureOpenAI.ApiKey) &&
                !string.IsNullOrWhiteSpace(_aiServiceSettings.AzureOpenAI.Endpoint) &&
                !string.IsNullOrWhiteSpace(_aiServiceSettings.AzureOpenAI.DeploymentName))
            {
                builder.AddAzureOpenAIChatCompletion(
                    deploymentName: _aiServiceSettings.AzureOpenAI.DeploymentName,
                    apiKey: _aiServiceSettings.AzureOpenAI.ApiKey,
                    endpoint: _aiServiceSettings.AzureOpenAI.Endpoint,
                    modelId: _aiServiceSettings.AzureOpenAI.ModelId ?? string.Empty,
                    serviceId: "azure");
                servicesAdded++;
            }

            // Add HuggingFace if configured with API key
            if (_aiServiceSettings.HuggingFace != null &&
                !string.IsNullOrWhiteSpace(_aiServiceSettings.HuggingFace.ApiKey) &&
                !string.IsNullOrWhiteSpace(_aiServiceSettings.HuggingFace.ModelId))
            {
                builder.AddHuggingFaceChatCompletion(
                    model: _aiServiceSettings.HuggingFace.ModelId,
                    apiKey: _aiServiceSettings.HuggingFace.ApiKey,
                    serviceId: "huggingface");
                servicesAdded++;
            }

            if (servicesAdded == 0)
            {
                throw new InvalidOperationException(
                    "No AI services are properly configured. At least one service must be configured with valid settings.");
            }

            builder.Services.AddSingleton(_loggerFactory);

            return builder.Build();
        }
    }
}
