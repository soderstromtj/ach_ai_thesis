using NIU.ACH_AI.Application.Configuration;
using NIU.ACH_AI.Application.DTOs;
using NIU.ACH_AI.Domain.Entities;

namespace NIU.ACH_AI.Application.Interfaces
{
    /// <summary>
    /// Defines the contract for presenting results to the user or output.
    /// </summary>
    public interface IResultPresenter
    {
        /// <summary>
        /// Displays the summary information for an experiment.
        /// </summary>
        /// <param name="experimentConfiguration">The experiment configuration.</param>
        void DisplayExperimentInfo(ExperimentConfiguration experimentConfiguration);

        /// <summary>
        /// Displays a list of hypotheses.
        /// </summary>
        /// <param name="title">The section title.</param>
        /// <param name="hypotheses">The collection of hypotheses to display.</param>
        void DisplayHypotheses(string title, IEnumerable<Hypothesis> hypotheses);

        /// <summary>
        /// Displays a list of evidence.
        /// </summary>
        /// <param name="title">The section title.</param>
        /// <param name="evidence">The collection of evidence to display.</param>
        void DisplayEvidence(string title, IEnumerable<Evidence> evidence);

        /// <summary>
        /// Displays a single evaluation result.
        /// </summary>
        /// <param name="evaluation">The evaluation to display.</param>
        void DisplayEvaluation(EvidenceHypothesisEvaluation evaluation);

        /// <summary>
        /// Displays an error message.
        /// </summary>
        /// <param name="message">The error message content.</param>
        void DisplayErrorMessage(string message);

        /// <summary>
        /// Displays the full workflow result.
        /// </summary>
        /// <param name="result">The workflow result to display.</param>
        void DisplayWorkflowResult(ACHWorkflowResult result);
    }
}
