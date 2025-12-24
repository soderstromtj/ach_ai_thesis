using NIU.ACH_AI.Domain.Entities;

namespace NIU.ACH_AI.Application.DTOs
{
    /// <summary>
    /// Represents the result of an ACH workflow execution.
    /// Contains all outputs from each step of the workflow.
    /// </summary>
    public class ACHWorkflowResult
    {
        /// <summary>
        /// The experiment ID
        /// </summary>
        public string ExperimentId { get; set; } = string.Empty;

        /// <summary>
        /// The experiment name
        /// </summary>
        public string ExperimentName { get; set; } = string.Empty;

        /// <summary>
        /// Indicates whether the workflow completed successfully
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Error message if the workflow failed
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// Initial hypotheses generated from brainstorming step
        /// </summary>
        public List<Hypothesis>? Hypotheses { get; set; }

        /// <summary>
        /// Refined hypotheses from evaluation/refinement step
        /// </summary>
        public List<Hypothesis>? RefinedHypotheses { get; set; }

        /// <summary>
        /// Evidence extracted from the context
        /// </summary>
        public List<Evidence>? Evidence { get; set; }

        /// <summary>
        /// Evaluations of evidence against hypotheses
        /// </summary>
        public List<EvidenceHypothesisEvaluation>? Evaluations { get; set; }
    }
}
