namespace NIU.ACH_AI.Infrastructure.Persistence.Entities
{
    /// <summary>
    /// Database entity for Hypothesis table
    /// </summary>
    public class HypothesisEntity
    {
        public Guid HypothesisId { get; set; }
        public Guid StepExecutionId { get; set; }
        public string ShortTitle { get; set; } = string.Empty;
        public string HypothesisText { get; set; } = string.Empty;
        public bool IsRefined { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
