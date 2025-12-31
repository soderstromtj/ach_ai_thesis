namespace NIU.ACH_AI.Application.DTOs
{
    /// <summary>
    /// Captures a single agent response for persistence and analysis purposes.
    /// </summary>
    /// <remarks>
    /// This record stores metadata about the interaction, including token usage and timing.
    /// </remarks>
    public class AgentResponseRecord
    {
        /// <summary>
        /// Gets the unique identifier for the specific step execution.
        /// </summary>
        public Guid StepExecutionId { get; init; }

        /// <summary>
        /// Gets the identifier of the agent configuration used.
        /// </summary>
        public Guid AgentConfigurationId { get; init; }

        /// <summary>
        /// Gets the name of the agent responsible for the response.
        /// </summary>
        public string AgentName { get; init; } = string.Empty;

        /// <summary>
        /// Gets the number of tokens used in the prompt.
        /// </summary>
        public int? InputTokenCount { get; init; }

        /// <summary>
        /// Gets the number of tokens generated in the response.
        /// </summary>
        public int? OutputTokenCount { get; init; }

        /// <summary>
        /// Gets the length of the content string.
        /// </summary>
        public int? ContentLength { get; init; }

        /// <summary>
        /// Gets the actual text content of the response.
        /// </summary>
        public string? Content { get; init; }

        /// <summary>
        /// Gets the turn number in a multi-turn conversation.
        /// </summary>
        public int? TurnNumber { get; init; }

        /// <summary>
        /// Gets the duration of the response generation in milliseconds.
        /// </summary>
        public long? ResponseDuration { get; init; }

        /// <summary>
        /// Gets the timestamp when record was created (defaults to UTC now).
        /// </summary>
        public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

        // New extended metadata fields
        /// <summary>
        /// Gets the unique identifier for the completion from the LLM provider.
        /// </summary>
        public string? CompletionId { get; init; }

        /// <summary>
        /// Gets the number of tokens used for reasoning/chain-of-thought.
        /// </summary>
        public int? ReasoningTokenCount { get; init; }

        /// <summary>
        /// Gets the number of tokens used for output audio.
        /// </summary>
        public int? OutputAudioTokenCount { get; init; }

        /// <summary>
        /// Gets the number of tokens for accepted predictions.
        /// </summary>
        public int? AcceptedPredictionTokenCount { get; init; }

        /// <summary>
        /// Gets the number of tokens for rejected predictions.
        /// </summary>
        public int? RejectedPredictionTokenCount { get; init; }

        /// <summary>
        /// Gets the number of tokens used for input audio.
        /// </summary>
        public int? InputAudioTokenCount { get; init; }

        /// <summary>
        /// Gets the number of cached input tokens.
        /// </summary>
        public int? CachedInputTokenCount { get; init; }

        /// <summary>
        /// Gets the timestamp when the generation finished.
        /// </summary>
        public DateTime FinishedAt { get; init; }
    }
}
