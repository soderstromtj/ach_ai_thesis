using NIU.ACH_AI.Domain.Entities;

namespace NIU.ACH_AI.Application.DTOs
{
    /// <summary>
    /// Aggregates the final output of an Analysis of Competing Hypotheses (ACH) workflow execution.
    /// </summary>
    /// <remarks>
    /// Consolidates generated hypotheses, extracted evidence, and evaluation results into a single object for persistence or client consumption.
    /// </remarks>
    public class ACHWorkflowResult
    {
        /// <summary>
        /// Gets or sets the global identifier linking this result to a specific test suite.
        /// </summary>
        public string ExperimentId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the human-readable title of the test suite.
        /// </summary>
        public string ExperimentName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets a value indicating whether the workflow executed without fatal errors.
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Gets or sets the failure cause, if the workflow did not complete successfully.
        /// </summary>
        /// <value>The error details; otherwise, <c>null</c>.</value>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// Gets or sets the raw, unrefined ideas produced during initial brainstorming.
        /// </summary>
        public List<Hypothesis>? Hypotheses { get; set; }

        /// <summary>
        /// Gets or sets the consolidated and vetted ideas ready for evidence evaluation.
        /// </summary>
        public List<Hypothesis>? RefinedHypotheses { get; set; }

        /// <summary>
        /// Gets or sets the factual data points sourced from the provided document context.
        /// </summary>
        public List<Evidence>? Evidence { get; set; }

        /// <summary>
        /// Gets or sets the scored relationships determining how well each piece of evidence supports each hypothesis.
        /// </summary>
        public List<EvidenceHypothesisEvaluation>? Evaluations { get; set; }
    }
}
