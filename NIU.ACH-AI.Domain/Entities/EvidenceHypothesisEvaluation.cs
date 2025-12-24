using NIU.ACH_AI.Domain.ValueObjects;

namespace NIU.ACH_AI.Domain.Entities
{
    public class EvidenceHypothesisEvaluation
    {
        public Hypothesis Hypothesis { get; set; } = new Hypothesis();
        public Evidence Evidence { get; set; } = new Evidence();
        public EvaluationScore Score { get; set; }
        public string ScoreRationale { get; set; } = string.Empty;
        public decimal ConfidenceLevel { get; set; }
        public string ConfidenceRationale { get; set; } = string.Empty;

        public override string ToString()
        {
            return $"Hypothesis: {Hypothesis.HypothesisText}\n" +
                   $"Evidence: {Evidence.Claim}\n" +
                   $"Score: {Score}\n" +
                   $"Score Rationale: {ScoreRationale}\n" +
                   $"Confidence Level: {ConfidenceLevel}\n" +
                   $"Confidence Rationale: {ConfidenceRationale}";
        }
    }
}
