using NIU.ACH_AI.Domain.Entities;

namespace NIU.ACH_AI.Application.DTOs
{
    /// <summary>
    /// Represents the aggregated result of an Analysis of Competing Hypotheses (ACH) workflow execution.
    /// </summary>
    /// <remarks>
    /// This object contains the cumulative outputs from all steps of the workflow, including hypotheses, evidence, and evaluations.
    /// </remarks>
    public class ACHWorkflowResult
    {
        /// <summary>
        /// Gets or sets the unique identifier for the experiment.
        /// </summary>
        public string ExperimentId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the user-friendly name of the experiment.
        /// </summary>
        public string ExperimentName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets a value indicating whether the workflow completed successfully.
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Gets or sets the error message if the workflow failed.
        /// </summary>
        /// <value>
        /// The error details if <see cref="Success"/> is <c>false</c>; otherwise, <c>null</c>.
        /// </value>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// Gets or sets the initial hypotheses generated during the brainstorming phase.
        /// </summary>
        public List<Hypothesis>? Hypotheses { get; set; }

        /// <summary>
        /// Gets or sets the refined hypotheses resulting from the selection and refinement phase.
        /// </summary>
        public List<Hypothesis>? RefinedHypotheses { get; set; }

        /// <summary>
        /// Gets or sets the evidence extracted from the provided context.
        /// </summary>
        public List<Evidence>? Evidence { get; set; }

        /// <summary>
        /// Gets or sets the evaluations of evidence against the refined hypotheses.
        /// </summary>
        public List<EvidenceHypothesisEvaluation>? Evaluations { get; set; }
    }
}
