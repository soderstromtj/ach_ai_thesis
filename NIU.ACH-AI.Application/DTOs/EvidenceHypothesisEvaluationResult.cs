using NIU.ACH_AI.Domain.Entities;

namespace NIU.ACH_AI.Application.DTOs
{
    public class EvidenceHypothesisEvaluationResult
    {
        public List<EvidenceHypothesisEvaluation> Evaluations { get; set; } = new List<EvidenceHypothesisEvaluation>();

        public override string ToString()
        {
            // Display the hypothesis, followed by the evidence, followed by the scores, and a bulleted list of rationales
            string result = string.Empty;

            // Get the hypothesis from the first evaluation (assuming all evaluations are for the same hypothesis)
            if (Evaluations.Count > 0)
            {
                result += $"Hypothesis:\n{Evaluations[0].Hypothesis}\n\n";
            }

            // Get the evidence from the first evaluation (assuming all evaluations are for the same evidence)
            if (Evaluations.Count > 0)
            {
                result += $"Evidence:\n{Evaluations[0].Evidence}\n\n";
            }

            // Now display each evaluation score in a comma-separated list
            result += "Evaluation Scores: ";
            result += string.Join(", ", Evaluations.Select(e => e.Score.ToString()));

            // Now display each rationale in a bulleted list
            result += "\n\nRationales:\n";
            foreach (var evaluation in Evaluations)
            {
                result += $"- {evaluation.ScoreRationale}\n";
            }

            return result;
        }
    }
}
