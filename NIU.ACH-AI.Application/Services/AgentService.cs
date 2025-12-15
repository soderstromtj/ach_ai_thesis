using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using NIU.ACHAI.Application.Configuration;
using NIU.ACHAI.Application.Services.KernelBuilders;

namespace NIU.ACHAI.Application.Services
{
    public class AgentService : IAgentService
    {
        private readonly IEnumerable<AgentConfiguration> _agentConfigurations;
        private readonly AIServiceSettings _aiServiceSettings;
        private readonly ILoggerFactory _loggerFactory;
        private readonly ILogger _logger;

        public AgentService(
            IEnumerable<AgentConfiguration> agentConfigurations,
            AIServiceSettings aiServiceSettings,
            ILoggerFactory loggerFactory)
        {
            _agentConfigurations = agentConfigurations;
            _aiServiceSettings = aiServiceSettings;
            _loggerFactory = loggerFactory;
            _logger = loggerFactory.CreateLogger<AgentService>();
        }

        IEnumerable<Agent> IAgentService.CreateAgents()
        {
            _logger.LogDebug($"Current class: {nameof(AgentService)}\tMessage: Creating agents based on configuration.");

            // Create agents based on the configurations from appsettings
            List<ChatCompletionAgent> agents = _agentConfigurations.Select(config =>
            {
                // Build a kernel for this agent based on its ServiceId and ModelId
                var kernel = BuildKernelForAgent(config);

                var agent = new ChatCompletionAgent
                {
                    Name = config.Name,
                    Description = config.Description,
                    Instructions = config.Instructions,
                    Kernel = kernel
                };

                return agent;
            }).ToList();

            _logger.LogDebug($"Current class: {nameof(AgentService)}\tMessage: Created {agents.Count} agents. Agent names are: {string.Join(", ", agents.Select(a => a.Name))}");

            return agents;
        }

        private Kernel BuildKernelForAgent(AgentConfiguration agentConfig)
        {
            var serviceId = agentConfig.ServiceId;
            var modelIdOverride = agentConfig.ModelId;

            _logger.LogDebug($"Current class: {nameof(AgentService)}\tMessage: Building kernel for agent '{agentConfig.Name}' with ServiceId: '{serviceId ?? "openai (default)"}', ModelId: '{modelIdOverride ?? "default"}'.");

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

            _logger.LogDebug($"Current class: {nameof(AgentService)}\tMessage: Using '{adapter.SupportedProvider}' adapter to build kernel.");

            return adapter.BuildKernel(modelIdOverride);
        }

        private IKernelBuilderAdapter CreateOpenAIAdapter()
        {
            if (_aiServiceSettings.OpenAI == null || string.IsNullOrWhiteSpace(_aiServiceSettings.OpenAI.ApiKey))
            {
                throw new InvalidOperationException(
                    "OpenAI service is not configured. Please ensure AIServiceSettings.OpenAI is properly configured in appsettings.");
            }

            var adapter = new OpenAIKernelAdapter(_aiServiceSettings.OpenAI, _aiServiceSettings, _loggerFactory);

            _logger.LogDebug($"Current class: {nameof(AgentService)}\tMessage: OpenAI adapter created successfully.");

            return adapter;
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

            var adapter = new AzureOpenAIKernelAdapter(_aiServiceSettings.AzureOpenAI, _aiServiceSettings, _loggerFactory);

            _logger.LogDebug($"Current class: {nameof(AgentService)}\tMessage: AzureOpenAI adapter created successfully.");

            return adapter;
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

            var adapter = new OllamaKernelAdapter(_aiServiceSettings.Ollama, _aiServiceSettings, _loggerFactory);

            _logger.LogDebug($"Current class: {nameof(AgentService)}\tMessage: Ollama adapter created successfully.");

            return adapter;
        }
    }
}
