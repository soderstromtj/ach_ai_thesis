using NIU.ACH_AI.Domain.Entities;

namespace NIU.ACH_AI.Application.Messaging.Events
{
    /// <summary>
    /// Serves as the completion payload for a factual data gathering phase.
    /// </summary>
    /// <remarks>
    /// Broadcast to transition the workflow state and provide concrete data points for subsequent scoring.
    /// </remarks>
    public interface IEvidenceExtractionResult
    {
         Guid ExperimentId { get; }
         Guid StepExecutionId { get; }
         List<Evidence> Evidence { get; }
         bool Success { get; }
         string? ErrorMessage { get; }
    }
}
