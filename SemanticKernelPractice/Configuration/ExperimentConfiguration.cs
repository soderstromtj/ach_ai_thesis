namespace SemanticKernelPractice.Configuration
{
    /// <summary>
    /// Runtime configuration that combines global AI service settings
    /// with experiment-specific settings (provider selection, agents, orchestration).
    /// This is what gets injected into services at runtime.
    /// </summary>
    public class ExperimentConfiguration
    {
        /// <summary>
        /// The name of the experiment being run
        /// </summary>
        public string ExperimentName { get; set; } = string.Empty;

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
        /// Global AI service provider configurations
        /// </summary>
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
