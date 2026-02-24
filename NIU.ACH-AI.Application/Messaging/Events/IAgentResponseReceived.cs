namespace NIU.ACH_AI.Application.Messaging.Events
{
    /// <summary>
    /// Serves as the notification that a participating actor has finalized a conversational turn.
    /// </summary>
    /// <remarks>
    /// Broadcast to enable granular progress tracking, telemetry recording, and real-time user interface updates.
    /// </remarks>
    public interface IAgentResponseReceived
    {
        Guid ExperimentId { get; }
        Guid StepExecutionId { get; }
        Guid AgentConfigurationId { get; }
        string AgentName { get; }
        string Content { get; }
        IReadOnlyDictionary<string, object?>? Metadata { get; }
        int TurnNumber { get; }
        long ResponseDurationMs { get; }
        DateTime Timestamp { get; }
    }
}
