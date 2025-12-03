namespace SemanticKernelPractice.Models
{
    /// <summary>
    /// Represents a single event in the workflow orchestration process.
    /// </summary>
    public class WorkflowEvent
    {
        /// <summary>
        /// When the event occurred
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Type of workflow event
        /// </summary>
        public WorkflowEventType EventType { get; set; }

        /// <summary>
        /// Name of the agent involved in this event (if applicable)
        /// </summary>
        public string? AgentName { get; set; }

        /// <summary>
        /// Reason or explanation for this event (e.g., why an agent was selected)
        /// </summary>
        public string? Reason { get; set; }

        /// <summary>
        /// Content of the event (e.g., agent response text)
        /// </summary>
        public string? Content { get; set; }

        /// <summary>
        /// Current turn number in the conversation
        /// </summary>
        public int? TurnNumber { get; set; }

        /// <summary>
        /// Duration of the operation in milliseconds (if applicable)
        /// </summary>
        public long? DurationMs { get; set; }

        /// <summary>
        /// Number of tokens used (if applicable)
        /// </summary>
        public int? TokenCount { get; set; }

        /// <summary>
        /// Additional metadata for this event
        /// </summary>
        public Dictionary<string, object>? Metadata { get; set; }
    }
}
