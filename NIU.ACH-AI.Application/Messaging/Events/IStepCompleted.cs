using NIU.ACH_AI.Domain.Entities;

namespace NIU.ACH_AI.Application.Messaging.Events
{
    /// <summary>
    /// Generic event fired when any ACH step completes.
    /// Payload varies based on the step type (e.g. List of Hypotheses).
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
