using System.Net.Http;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using NIU.ACH_AI.Application.Configuration;
using NIU.ACH_AI.Application.Interfaces;
using NIU.ACH_AI.Infrastructure.AI.KernelAdapters;
using NIU.ACH_AI.Infrastructure.Configuration;

namespace NIU.ACH_AI.Infrastructure.AI.Services
{
    /// <summary>
    /// Service for creating and configuring AI agents based on application settings.
    /// </summary>
    public class AgentService : IAgentService
    {
        private readonly IEnumerable<AgentConfiguration> _agentConfigurations;
        private readonly AIServiceSettings _aiServiceSettings;
        private readonly ILoggerFactory _loggerFactory;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IAgentConfigurationPersistence _agentConfigurationPersistence;
        private readonly ILogger _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="AgentService"/> class.
        /// </summary>
        /// <param name="agentConfigurations">Configurations for the agents to be created.</param>
        /// <param name="aiServiceSettings">Global AI service settings.</param>
        /// <param name="loggerFactory">Factory for creating loggers.</param>
        /// <param name="httpClientFactory">Factory for creating HttpClient instances.</param>
        public AgentService(
            IEnumerable<AgentConfiguration> agentConfigurations,
            AIServiceSettings aiServiceSettings,
            ILoggerFactory loggerFactory,
            IHttpClientFactory httpClientFactory,
            IAgentConfigurationPersistence agentConfigurationPersistence)
        {
            _agentConfigurations = agentConfigurations;
            _aiServiceSettings = aiServiceSettings;
            _loggerFactory = loggerFactory;
            _httpClientFactory = httpClientFactory;
            _agentConfigurationPersistence = agentConfigurationPersistence ?? throw new ArgumentNullException(nameof(agentConfigurationPersistence));
            _logger = loggerFactory.CreateLogger<AgentService>();
        }

        (IEnumerable<Agent> Agents, Dictionary<string, Guid> ConfigurationIds) IAgentService.CreateAgents(Guid? stepExecutionId)
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

            var configurationIds = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);

            if (stepExecutionId.HasValue)
            {
                var configsToPersist = _agentConfigurations.Select(c => new AgentConfiguration 
                {
                    Name = c.Name ?? "Unknown",
                    Description = c.Description ?? string.Empty,
                    Instructions = c.Instructions ?? string.Empty,
                    ServiceId = c.ServiceId ?? "openai",
                    ModelId = c.ModelId ?? "gpt-4o"
                });

                // Since create agent configs is async, we have to block here because IAgentService interface is synchronous
                // Alternatively, IAgentService should be async.
                // Checking interface... it is synchronous in my previous view. 
                // However, persistence is async.
                // Refactoring interface to Task<...> would be better but requires updates everywhere.
                // For now, I will use .GetAwaiter().GetResult() as a pragmatic step, or update interface to Async.
                // Given "clean code" I should make it Async. But that is a ripple effect.
                // The interface DOES NOT return Task. I will update it to Task if the user allows, 
                // BUT "call task_boundary" suggests I should stick to plan.
                // Wait, in my plan I wrote: "(IEnumerable<Agent> Agents, Dictionary<string, Guid> ConfigurationIds) CreateAgents(Guid? stepExecutionId = null)".
                // I did NOT specify Task.
                // I will use .GetAwaiter().GetResult() for now to minimize changes, noting it's a synchronous wrapper.
                // Or I can just fire and forget? No, we need the IDs.
                
                try
                {
                     var result = _agentConfigurationPersistence.CreateAgentConfigurationsAsync(
                        stepExecutionId.Value, 
                        configsToPersist, 
                        CancellationToken.None).GetAwaiter().GetResult();
                        
                     configurationIds = new Dictionary<string, Guid>(result);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to persist agent configurations synchronously.");
                    // We continue without IDs, meaning persistence downstream might fail.
                }
            }

            _logger.LogDebug($"Current class: {nameof(AgentService)}\tMessage: Created {agents.Count} agents. Agent names are: {string.Join(", ", agents.Select(a => a.Name))}");

            return (agents, configurationIds);
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

            var adapter = new OpenAIKernelAdapter(_aiServiceSettings.OpenAI, _aiServiceSettings, _loggerFactory, _httpClientFactory);

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

            var adapter = new AzureOpenAIKernelAdapter(_aiServiceSettings.AzureOpenAI, _aiServiceSettings, _loggerFactory, _httpClientFactory);

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

            var adapter = new OllamaKernelAdapter(_aiServiceSettings.Ollama, _aiServiceSettings, _loggerFactory, _httpClientFactory);

            _logger.LogDebug($"Current class: {nameof(AgentService)}\tMessage: Ollama adapter created successfully.");

            return adapter;
        }
    }
}
