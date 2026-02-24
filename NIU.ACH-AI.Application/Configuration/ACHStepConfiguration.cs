namespace NIU.ACH_AI.Application.Configuration
{
    /// <summary>
    /// Defines the configuration settings required to execute a single phase of the Analysis of Competing Hypotheses (ACH) process.
    /// </summary>
    /// <remarks>
    /// Encapsulates agent setups, orchestration rules, and instructions necessary to run a specific portion of the overarching experiment.
    /// </remarks>
    public class ACHStepConfiguration
    {
        /// <summary>
        /// Gets or sets the numeric identifier used to uniquely track this phase within the broader experiment.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the programmatic identifier used for routing or internal referencing (e.g., "MultiAgentChat").
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the human-readable explanation of the objective to inform analysts.
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the core prompt directives that guide the autonomous workflow toward its objective.
        /// </summary>
        public string TaskInstructions { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the definitions for the specific AI personas and capabilities enlisted for this phase.
        /// </summary>
        public AgentConfiguration[] AgentConfigurations { get; set; } = Array.Empty<AgentConfiguration>();

        /// <summary>
        /// Gets or sets the execution limits and behavioral rules to ensure stable processing.
        /// </summary>
        public OrchestrationSettings OrchestrationSettings { get; set; } = new();
    }
}
