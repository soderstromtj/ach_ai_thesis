using NIU.ACH_AI.Domain.Entities;

namespace NIU.ACH_AI.Application.Messaging.Events
{
    /// <summary>
    /// Signals that a specific ACH workflow step has finished.
    /// The result payload changes depending on which step was running (e.g., returning a list of hypotheses).
    /// </summary>
    public interface IStepCompleted<TResult>
    {
        Guid ExperimentId { get; }
        Guid StepExecutionId { get; }
        string StepName { get; }
        TResult Result { get; }
        DateTime Timestamp { get; }
    }
}
