namespace NIU.ACH_AI.Application.DTOs
{
    /// <summary>
    /// Contains the contextual information for the execution of a specific workflow step.
    /// </summary>
    public class StepExecutionContext
    {
        /// <summary>
        /// Gets or sets the unique identifier for the overall experiment.
        /// </summary>
        public Guid ExperimentId { get; set; }

        /// <summary>
        /// Gets or sets the unique identifier for this specific step execution instance.
        /// </summary>
        public Guid StepExecutionId { get; set; }

        /// <summary>
        /// Gets or sets the integer identifier of the ACH step.
        /// </summary>
        public int AchStepId { get; set; }

        /// <summary>
        /// Gets or sets the name or description of the ACH step.
        /// </summary>
        public string AchStepName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the execution status (e.g. "Completed", "Failed").
        /// </summary>
        public string Status { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the dictionary mapping agent role names to their configuration IDs.
        /// </summary>
        public Dictionary<string, Guid> AgentConfigurationIds { get; set; }
            = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);
    }
}
