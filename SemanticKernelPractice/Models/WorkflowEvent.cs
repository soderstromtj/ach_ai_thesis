namespace SemanticKernelPractice.Models
{
    /// <summary>
    /// Types of events that can occur during workflow orchestration.
    /// </summary>
    public enum WorkflowEventType
    {
        /// <summary>Orchestration has started</summary>
        OrchestrationStarted,

        /// <summary>An agent has been selected to respond</summary>
        AgentSelected,

        /// <summary>An agent has provided a response</summary>
        AgentResponseReceived,

        /// <summary>A handoff decision was made between agents</summary>
        HandoffDecision,

        /// <summary>Termination condition was evaluated</summary>
        TerminationCheck,

        /// <summary>Results were filtered or processed</summary>
        ResultFiltered,

        /// <summary>Orchestration has completed successfully</summary>
        OrchestrationCompleted,

        /// <summary>An error occurred during orchestration</summary>
        Error,

        /// <summary>Runtime lifecycle event</summary>
        RuntimeEvent,

        /// <summary>User input was requested or received</summary>
        UserInputEvent
    }

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
