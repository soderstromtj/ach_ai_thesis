namespace NIU.ACH_AI.Application.DTOs
{
    public class TokenUsageInfo
    {
        public int? InputTokenCount { get; set; }
        public int? OutputTokenCount { get; set; }
        public int? ReasoningTokenCount { get; set; }
        public int? OutputAudioTokenCount { get; set; }
        public int? AcceptedPredictionTokenCount { get; set; }
        public int? RejectedPredictionTokenCount { get; set; }
        public int? InputAudioTokenCount { get; set; }
        public int? CachedInputTokenCount { get; set; }
        public DateTimeOffset? CreatedAt { get; set; }
    }
}
