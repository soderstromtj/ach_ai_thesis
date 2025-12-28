namespace NIU.ACH_AI.Application.DTOs
{
    /// <summary>
    /// Captures a single agent response for persistence.
    /// </summary>
    public class AgentResponseRecord
    {
        public Guid StepExecutionId { get; init; }
        public Guid AgentConfigurationId { get; init; }
        public string AgentName { get; init; } = string.Empty;
        public int? InputTokenCount { get; init; }
        public int? OutputTokenCount { get; init; }
        public int? ContentLength { get; init; }
        public string? Content { get; init; }
        public int? TurnNumber { get; init; }
        public long? ResponseDuration { get; init; }
        public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    }
}
