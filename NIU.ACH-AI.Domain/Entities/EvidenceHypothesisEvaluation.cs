using NIU.ACH_AI.Domain.ValueObjects;

namespace NIU.ACH_AI.Domain.Entities
{
    public class EvidenceHypothesisEvaluation
    {
        public Hypothesis hypothesis { get; set; } = new Hypothesis();
        public Evidence evidence { get; set; } = new Evidence();
        public EvaluationScore score { get; set; }
        public string rationale { get; set; } = string.Empty;
    }
}
