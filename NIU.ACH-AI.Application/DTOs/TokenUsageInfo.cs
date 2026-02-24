namespace NIU.ACH_AI.Application.DTOs
{
    /// <summary>
    /// Details the cost and volume metrics returned by the LLM provider for a single generation request.
    /// </summary>
    public class TokenUsageInfo
    {
        /// <summary>
        /// Gets or sets the volume of text analyzed in the prompt payload.
        /// </summary>
        public int? InputTokenCount { get; set; }

        /// <summary>
        /// Gets or sets the volume of text generated in the completion payload.
        /// </summary>
        public int? OutputTokenCount { get; set; }

        /// <summary>
        /// Gets or sets the unseen tokens consumed by the provider's internal chain-of-thought logic.
        /// </summary>
        public int? ReasoningTokenCount { get; set; }

        /// <summary>
        /// Gets or sets the volume of generated multimodal audio content.
        /// </summary>
        public int? OutputAudioTokenCount { get; set; }

        /// <summary>
        /// Gets or sets the volume of speculative tokens explicitly validated by the model.
        /// </summary>
        public int? AcceptedPredictionTokenCount { get; set; }

        /// <summary>
        /// Gets or sets the volume of speculative tokens discarded by the model.
        /// </summary>
        public int? RejectedPredictionTokenCount { get; set; }

        /// <summary>
        /// Gets or sets the volume of analyzed multimodal audio content.
        /// </summary>
        public int? InputAudioTokenCount { get; set; }

        /// <summary>
        /// Gets or sets the prompt volume recognized and retrieved from the provider's short-term cache.
        /// </summary>
        public int? CachedInputTokenCount { get; set; }

        /// <summary>
        /// Gets or sets the precise system timestamp when the response metrics were finalized.
        /// </summary>
        public DateTimeOffset? CreatedAt { get; set; }
    }
}
