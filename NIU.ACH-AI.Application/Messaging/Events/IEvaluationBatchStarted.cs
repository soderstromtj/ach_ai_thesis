using NIU.ACH_AI.Application.Configuration;
using NIU.ACH_AI.Application.DTOs;

namespace NIU.ACH_AI.Application.Messaging.Events
{
    public interface IEvaluationBatchStarted
    {
        Guid ExperimentId { get; }
        Guid StepExecutionId { get; }
        int TotalEvaluations { get; }
    }
}
