using NIU.ACH_AI.Application.Configuration;
using NIU.ACH_AI.Application.DTOs;

namespace NIU.ACH_AI.Application.Messaging.Events
{
    /// <summary>
    /// Serves as the notification that a fan-out evaluation process has commenced.
    /// </summary>
    /// <remarks>
    /// Broadcast to inform tracking mechanisms of the expected volume of individual scoring operations.
    /// </remarks>
    public interface IEvaluationBatchStarted
    {
        Guid ExperimentId { get; }
        Guid StepExecutionId { get; }
        int TotalEvaluations { get; }
    }
}
