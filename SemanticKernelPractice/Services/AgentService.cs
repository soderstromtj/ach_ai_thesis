using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using SemanticKernelPractice.Configuration;
using SemanticKernelPractice.Services.KernelBuilders;

namespace SemanticKernelPractice.Services
{
    public class AgentService : IAgentService
    {
        private readonly IEnumerable<AgentConfiguration> _agentConfigurations;
        private readonly AIServiceSettings _aiServiceSettings;
        private readonly ILoggerFactory _loggerFactory;

        public AgentService(
            IEnumerable<AgentConfiguration> agentConfigurations,
            AIServiceSettings aiServiceSettings,
            ILoggerFactory loggerFactory)
        {
            _agentConfigurations = agentConfigurations;
            _aiServiceSettings = aiServiceSettings;
            _loggerFactory = loggerFactory;
        }

        IEnumerable<Agent> IAgentService.CreateAgents()
        {
            return _agentConfigurations.Select(config =>
            {
                // Build a kernel for this agent based on its ServiceId
                var kernel = BuildKernelForAgent(config.ServiceId);

                var agent = new ChatCompletionAgent
                {
                    Name = config.Name,
                    Description = config.Description,
                    Instructions = config.Instructions,
                    Kernel = kernel
                };

                return agent;
            }).ToList();
        }

        private Kernel BuildKernelForAgent(string? serviceId)
        {
            // Default to OpenAI if no ServiceId specified
            var effectiveServiceId = string.IsNullOrWhiteSpace(serviceId) ? "openai" : serviceId.ToLowerInvariant();

            IKernelBuilderAdapter adapter = effectiveServiceId switch
            {
                "openai" => CreateOpenAIAdapter(),
                "azure" => CreateAzureOpenAIAdapter(),
                "ollama" => CreateOllamaAdapter(),
                _ => throw new InvalidOperationException(
                    $"Unsupported ServiceId: '{serviceId}'. Valid values are: 'openai', 'azure', 'ollama'")
            };

            return adapter.BuildKernel();
        }

        private IKernelBuilderAdapter CreateOpenAIAdapter()
        {
            if (_aiServiceSettings.OpenAI == null || string.IsNullOrWhiteSpace(_aiServiceSettings.OpenAI.ApiKey))
            {
                throw new InvalidOperationException(
                    "OpenAI service is not configured. Please ensure AIServiceSettings.OpenAI is properly configured in appsettings.");
            }

            return new OpenAIKernelAdapter(_aiServiceSettings.OpenAI, _loggerFactory);
        }

        private IKernelBuilderAdapter CreateAzureOpenAIAdapter()
        {
            if (_aiServiceSettings.AzureOpenAI == null ||
                string.IsNullOrWhiteSpace(_aiServiceSettings.AzureOpenAI.ApiKey) ||
                string.IsNullOrWhiteSpace(_aiServiceSettings.AzureOpenAI.Endpoint) ||
                string.IsNullOrWhiteSpace(_aiServiceSettings.AzureOpenAI.DeploymentName))
            {
                throw new InvalidOperationException(
                    "Azure OpenAI service is not configured. Please ensure AIServiceSettings.AzureOpenAI is properly configured in appsettings.");
            }

            return new AzureOpenAIKernelAdapter(_aiServiceSettings.AzureOpenAI, _loggerFactory);
        }

        private IKernelBuilderAdapter CreateOllamaAdapter()
        {
            if (_aiServiceSettings.Ollama == null ||
                string.IsNullOrWhiteSpace(_aiServiceSettings.Ollama.Endpoint) ||
                string.IsNullOrWhiteSpace(_aiServiceSettings.Ollama.ModelId))
            {
                throw new InvalidOperationException(
                    "Ollama service is not configured. Please ensure AIServiceSettings.Ollama is properly configured in appsettings.");
            }

            return new OllamaKernelAdapter(_aiServiceSettings.Ollama, _loggerFactory);
        }
    }
}
