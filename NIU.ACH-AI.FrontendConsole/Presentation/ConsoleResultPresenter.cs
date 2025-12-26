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
        /// Displays basic information about the experiment information.
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
        /// Displays a list of hypotheses with their short titles and full texts.
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
        /// Displays a list of evidence items with their details.
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

        public void DisplayEvaluation(EvidenceHypothesisEvaluation evaluation)
        {
            ArgumentNullException.ThrowIfNull(evaluation);

            Console.WriteLine(evaluation.ToString());
        }

        public void DisplayErrorMessage(string message)
        {
            ArgumentException.ThrowIfNullOrEmpty(message?.Trim());

            Console.WriteLine($"ERROR: {message}");
        }

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
