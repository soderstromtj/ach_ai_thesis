using NIU.ACH_AI.Application.Configuration;
using NIU.ACH_AI.Application.DTOs;
using NIU.ACH_AI.Application.Interfaces;
using NIU.ACH_AI.Domain.Entities;

namespace NIU.ACH_AI.FrontendConsole.Presentation
{
    public class ConsoleResultPresenter : IResultPresenter
    {
        private const char Separator = '=';
        private const int SeparatorLength = 70;

        /// <summary>
        /// Shows basic information about the experiment.
        /// </summary>
        /// <param name="experimentConfiguration">The configuration for the experiment.</param>
        public void DisplayExperimentInfo(ExperimentConfiguration experimentConfiguration)
        {
            ArgumentNullException.ThrowIfNull(experimentConfiguration);
            ArgumentException.ThrowIfNullOrEmpty(experimentConfiguration.Name?.Trim());
            ArgumentException.ThrowIfNullOrEmpty(experimentConfiguration.KeyQuestion?.Trim());

            Console.WriteLine(experimentConfiguration.ToString());
            Console.WriteLine(new string(Separator, SeparatorLength));
        }

        /// <summary>
        /// Shows a list of hypotheses.
        /// </summary>
        /// <param name="title">A title that is displayed before the list of hypotheses.</param>
        /// <param name="hypotheses">The collection of hypotheses to display.</param>
        public void DisplayHypotheses(string title, IEnumerable<Hypothesis> hypotheses)
        {
            ArgumentNullException.ThrowIfNull(hypotheses);
            ArgumentException.ThrowIfNullOrEmpty(title?.Trim());

            Console.WriteLine(new string(Separator, SeparatorLength));
            Console.WriteLine(title);

            foreach (var hypothesis in hypotheses)
            {
                Console.WriteLine(hypothesis.ToString());
            }
        }

        /// <summary>
        /// Shows a list of evidence items.
        /// </summary>
        /// <param name="title">A title that is displayed before the list of evidence</param>
        /// <param name="evidence">The collection of evidence items to display.</param>
        public void DisplayEvidence(string title, IEnumerable<Evidence> evidence)
        {
            ArgumentNullException.ThrowIfNull(evidence);
            ArgumentException.ThrowIfNullOrEmpty(title?.Trim());

            Console.WriteLine(new string(Separator, SeparatorLength));
            Console.WriteLine(title);

            foreach (var ev in evidence)
            {
                Console.WriteLine(ev.ToString());
            }
        }

        /// <summary>
        /// Shows the evaluation of a piece of evidence against a hypothesis.
        /// </summary>
        /// <param name="evaluation">The evaluation of evidence against a hypothesis.</param>
        public void DisplayEvaluation(EvidenceHypothesisEvaluation evaluation)
        {
            ArgumentNullException.ThrowIfNull(evaluation);

            Console.WriteLine(evaluation.ToString());
        }

        /// <summary>
        /// Shows an error message in the console.
        /// </summary>
        /// <param name="message">The error message to display.</param>
        public void DisplayErrorMessage(string message)
        {
            ArgumentException.ThrowIfNullOrEmpty(message?.Trim());

            Console.WriteLine($"ERROR: {message}");
        }


        /// <summary>
        /// Shows the final results of the entire workflow.
        /// </summary>
        /// <param name="result">The workflow result to display.</param>
        public void DisplayWorkflowResult(ACHWorkflowResult result)
        {
            ArgumentNullException.ThrowIfNull(result);

            // Display results
            Console.WriteLine("\n=== Orchestration Workflow Completed ===\n");
            if (result.Success)
            {
                DisplayHypotheses("Initial Hypotheses:", result.Hypotheses ?? []);
                DisplayHypotheses("Refined Hypotheses:", result.RefinedHypotheses ?? []);
                DisplayEvidence("Extracted Evidence:", result.Evidence ?? []);
            }
            else
            {
                Console.WriteLine($"Workflow failed with error: {result.ErrorMessage}");
            }
        }
    }
}
