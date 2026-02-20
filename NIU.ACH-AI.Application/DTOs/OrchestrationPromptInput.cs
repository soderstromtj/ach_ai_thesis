namespace NIU.ACH_AI.Application.DTOs
{
    /// <summary>
    /// Represents the input required to construct a prompt for the orchestration engine.
    /// </summary>
    /// <remarks>
    /// This class aggregates all necessary context, instructions, and intermediate results needed by the AI to perform a step.
    /// </remarks>
    public class OrchestrationPromptInput
    {
        /// <summary>
        /// Gets or sets the main intelligence question or problem statement.
        /// </summary>
        public string KeyQuestion { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the background information or data source content.
        /// </summary>
        public string Context { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the specific instructions for the current task.
        /// </summary>
        public string TaskInstructions { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the results from the hypothesis generation step, if available.
        /// </summary>
        public HypothesisResult? HypothesisResult { get; set; } = null;

        /// <summary>
        /// Gets or sets the results from the evidence extraction step, if available.
        /// </summary>
        public EvidenceResult? EvidenceResult { get; set; } = null;

        /// <summary>
        /// Gets or sets any additional or custom instructions for the agent.
        /// </summary>
        public string AdditionalInstructions { get; set; } = string.Empty;
    }
}
