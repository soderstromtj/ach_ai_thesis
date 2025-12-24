using NIU.ACH_AI.Domain.Entities;

namespace NIU.ACH_AI.Application.DTOs
{
    public class EvidenceHypothesisEvaluationResult
    {
        public List<EvidenceHypothesisEvaluation> Evaluations { get; set; } = new List<EvidenceHypothesisEvaluation>();

        public override string ToString()
        {
            // Neatly format the evaluations list for display
            return string.Join(Environment.NewLine + Environment.NewLine, Evaluations.Select((e, index) =>
                $"{index + 1}. {e.ToString()}"));
        }
    }
}
