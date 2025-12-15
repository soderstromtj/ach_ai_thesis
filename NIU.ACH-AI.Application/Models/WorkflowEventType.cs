namespace NIU.ACHAI.Application.Models
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
}
