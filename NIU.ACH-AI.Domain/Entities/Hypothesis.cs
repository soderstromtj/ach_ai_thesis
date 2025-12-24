namespace NIU.ACH_AI.Domain.Entities
{
    public class Hypothesis
    {
        public string ShortTitle { get; set; } = string.Empty;
        public string HypothesisText { get; set; } = string.Empty;

        public override string ToString()
        {
            return $"{ShortTitle}. {HypothesisText}";
        }
    }
}
