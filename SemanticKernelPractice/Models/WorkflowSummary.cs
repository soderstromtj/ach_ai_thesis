namespace SemanticKernelPractice.Models
{
    /// <summary>
    /// Summary statistics for a completed workflow orchestration.
    /// </summary>
    public class WorkflowSummary
    {
        /// <summary>
        /// When the orchestration started
        /// </summary>
        public DateTime StartTime { get; set; }

        /// <summary>
        /// When the orchestration ended
        /// </summary>
        public DateTime EndTime { get; set; }

        /// <summary>
        /// Total duration in milliseconds
        /// </summary>
        public long DurationMs => (long)(EndTime - StartTime).TotalMilliseconds;

        /// <summary>
        /// Total number of turns/messages
        /// </summary>
        public int TotalTurns { get; set; }

        /// <summary>
        /// Total tokens used across all agents
        /// </summary>
        public int TotalTokens { get; set; }

        /// <summary>
        /// How the orchestration ended (completion phrase, max turns, error, etc.)
        /// </summary>
        public string? TerminationReason { get; set; }

        /// <summary>
        /// Number of items extracted (for evidence extraction)
        /// </summary>
        public int? ResultCount { get; set; }

        /// <summary>
        /// Breakdown of turns by agent
        /// </summary>
        public Dictionary<string, int> TurnsByAgent { get; set; } = new();

        /// <summary>
        /// Breakdown of tokens by agent
        /// </summary>
        public Dictionary<string, int> TokensByAgent { get; set; } = new();
    }
}
