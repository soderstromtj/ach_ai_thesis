namespace NIU.ACH_AI.Application.DTOs
{
    /// <summary>
    /// Carries runtime state and configuration identifiers throughout the lifecycle of a specific workflow phase.
    /// </summary>
    public class StepExecutionContext
    {
        /// <summary>
        /// Gets or sets the global identifier linking this execution to the parent test suite.
        /// </summary>
        public Guid ExperimentId { get; set; }

        /// <summary>
        /// Gets or sets the specific tracking identifier for this distinct run of the phase.
        /// </summary>
        public Guid StepExecutionId { get; set; }

        /// <summary>
        /// Gets or sets the numeric identifier denoting the position or type of the phase within the workflow.
        /// </summary>
        public int AchStepId { get; set; }

        /// <summary>
        /// Gets or sets the human-readable label or objective of the active phase.
        /// </summary>
        public string AchStepName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the current processing state flag (e.g. "Completed", "Failed").
        /// </summary>
        public string Status { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the resolution map linking logical agent roles to their concrete persona configurations.
        /// </summary>
        public Dictionary<string, Guid> AgentConfigurationIds { get; set; }
            = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);
    }
}
