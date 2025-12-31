namespace NIU.ACH_AI.Application.DTOs
{
    /// <summary>
    /// Encapsulates detailed token usage statistics offered by the LLM provider.
    /// </summary>
    public class TokenUsageInfo
    {
        /// <summary>
        /// Gets or sets the number of tokens in the prompt.
        /// </summary>
        public int? InputTokenCount { get; set; }

        /// <summary>
        /// Gets or sets the number of tokens in the completion.
        /// </summary>
        public int? OutputTokenCount { get; set; }

        /// <summary>
        /// Gets or sets the number of tokens used for reasoning tasks (if applicable).
        /// </summary>
        public int? ReasoningTokenCount { get; set; }

        /// <summary>
        /// Gets or sets the number of tokens used for generating audio output.
        /// </summary>
        public int? OutputAudioTokenCount { get; set; }

        /// <summary>
        /// Gets or sets the number of tokens associated with accepted predictions.
        /// </summary>
        public int? AcceptedPredictionTokenCount { get; set; }

        /// <summary>
        /// Gets or sets the number of tokens associated with rejected predictions.
        /// </summary>
        public int? RejectedPredictionTokenCount { get; set; }

        /// <summary>
        /// Gets or sets the number of tokens used for processing audio input.
        /// </summary>
        public int? InputAudioTokenCount { get; set; }

        /// <summary>
        /// Gets or sets the number of input tokens that utilized the cache.
        /// </summary>
        public int? CachedInputTokenCount { get; set; }

        /// <summary>
        /// Gets or sets the timestamp when the usage was recorded.
        /// </summary>
        public DateTimeOffset? CreatedAt { get; set; }
    }
}
