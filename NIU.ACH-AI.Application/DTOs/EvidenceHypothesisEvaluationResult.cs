using NIU.ACH_AI.Domain.Entities;

namespace NIU.ACH_AI.Application.DTOs
{
    /// <summary>
    /// Represents the result of an evaluation phase, containing a list of evidence-hypothesis evaluations.
    /// </summary>
    public class EvidenceHypothesisEvaluationResult
    {
        /// <summary>
        /// Gets or sets the list of evaluations performed.
        /// </summary>
        public List<EvidenceHypothesisEvaluation> Evaluations { get; set; } = new List<EvidenceHypothesisEvaluation>();

        /// <summary>
        /// Returns a string that represents the current object.
        /// </summary>
        /// <returns>
        /// A formatted string listing all evaluations or a default message if none exist.
        /// </returns>
        public override string ToString()
        {
            if (Evaluations == null || !Evaluations.Any())
            {
                return "No evaluations available.";
            }

            // Neatly format the evaluations list for display
            return string.Join(Environment.NewLine + Environment.NewLine, Evaluations.Select((e, index) =>
                $"{index + 1}. {e.ToString()}"));
        }
    }
}
