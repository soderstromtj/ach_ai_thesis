using System.Text.Json.Serialization;

namespace SemanticKernelPractice.Configuration
{
    /// <summary>
    /// Experiment configuration that can be deserialized from JSON and used at runtime.
    /// Combines experiment-specific settings (provider, agents, orchestration) with
    /// runtime-injected global AI service settings.
    /// </summary>
    public class ExperimentConfiguration
    {
        /// <summary>
        /// The name of the experiment
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Description of what this experiment tests or analyzes
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// The selected AI provider for this experiment
        /// </summary>
        public AIServiceProvider Provider { get; set; }

        /// <summary>
        /// The key intelligence question for the experiment
        /// </summary>
        public string KeyIntelligenceQuestion { get; set; } = string.Empty;

        /// <summary>
        /// The event context that agent analysts will analyze
        /// </summary>
        public string Context { get; set; } = string.Empty;

        /// <summary>
        /// Task instructions for the orchestration workflow
        /// </summary>
        public string TaskInstructions { get; set; } = string.Empty;

        /// <summary>
        /// Global AI service provider configurations (injected at runtime, not from JSON)
        /// </summary>
        [JsonIgnore]
        public AIServiceSettings GlobalAIServiceSettings { get; set; } = new();

        /// <summary>
        /// Agent configurations for this experiment
        /// </summary>
        public AgentConfiguration[] AgentConfigurations { get; set; } = Array.Empty<AgentConfiguration>();

        /// <summary>
        /// Orchestration settings for this experiment
        /// </summary>
        public OrchestrationSettings OrchestrationSettings { get; set; } = new();
    }
}
