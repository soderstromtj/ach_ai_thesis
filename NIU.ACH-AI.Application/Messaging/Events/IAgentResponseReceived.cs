namespace NIU.ACH_AI.Application.Messaging.Events
{
    /// <summary>
    /// Event fired when an individual agent generates a response (or a chunk of it).
    /// Used for granular persistence and UI updates.
    /// </summary>
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
