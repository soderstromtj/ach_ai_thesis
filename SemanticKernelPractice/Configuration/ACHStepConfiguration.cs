namespace SemanticKernelPractice.Configuration
{
    /// <summary>
    /// Configuration for an individual ACH step within an experiment.
    /// Contains all settings needed to execute a single step of the ACH process,
    /// including agent configurations, orchestration settings, and context variables.
    /// </summary>
    public class ACHStepConfiguration
    {
        /// <summary>
        /// Unique identifier for this ACH step
        /// </summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// The name of the ACH step (e.g., "MultiAgentChat")
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Description of what this ACH step accomplishes
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// The selected AI provider for this ACH step
        /// </summary>
        public AIServiceProvider Provider { get; set; }

        /// <summary>
        /// The key intelligence question for analysts to address
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
        /// Agent configurations for this ACH step
        /// </summary>
        public AgentConfiguration[] AgentConfigurations { get; set; } = Array.Empty<AgentConfiguration>();

        /// <summary>
        /// Orchestration settings for this ACH step
        /// </summary>
        public OrchestrationSettings OrchestrationSettings { get; set; } = new();
    }
}
