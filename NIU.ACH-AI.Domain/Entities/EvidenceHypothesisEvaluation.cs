using NIU.ACH_AI.Domain.ValueObjects;

namespace NIU.ACH_AI.Domain.Entities
{
    /// <summary>
    /// Represents the evaluation of a specific piece of <see cref="Evidence"/> against a specific <see cref="Hypothesis"/>.
    /// </summary>
    /// <remarks>
    /// This class captures how consistent a piece of evidence is with a hypothesis, along with the rationale and confidence level.
    /// </remarks>
    public class EvidenceHypothesisEvaluation
    {
        /// <summary>
        /// Gets or sets the hypothesis being evaluated.
        /// </summary>
        public Hypothesis Hypothesis { get; set; } = new Hypothesis();

        /// <summary>
        /// Gets or sets the evidence being evaluated against the hypothesis.
        /// </summary>
        public Evidence Evidence { get; set; } = new Evidence();

        /// <summary>
        /// Gets or sets the score indicating the consistency between the evidence and the hypothesis.
        /// </summary>
        public EvaluationScore Score { get; set; }

        /// <summary>
        /// Gets or sets the explanation for why the specific <see cref="Score"/> was assigned.
        /// </summary>
        public string ScoreRationale { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the confidence level in the evaluation, typically between 0.0 and 1.0.
        /// </summary>
        public decimal ConfidenceLevel { get; set; }

        /// <summary>
        /// Gets or sets the rationale for the assigned <see cref="ConfidenceLevel"/>.
        /// </summary>
        public string ConfidenceRationale { get; set; } = string.Empty;

        /// <summary>
        /// Returns a string that represents the current object.
        /// </summary>
        /// <returns>
        /// A string outlining the hypothesis, evidence, score, and rationales.
        /// </returns>
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
