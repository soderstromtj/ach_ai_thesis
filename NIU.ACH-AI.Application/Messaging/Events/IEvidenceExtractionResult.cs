using NIU.ACH_AI.Domain.Entities;

namespace NIU.ACH_AI.Application.Messaging.Events
{
    /// <summary>
    /// Result event for the Evidence Extraction step.
    /// </summary>
    public interface IEvidenceExtractionResult
    {
         Guid ExperimentId { get; }
         Guid StepExecutionId { get; }
         List<Evidence> Evidence { get; }
         bool Success { get; }
         string? ErrorMessage { get; }
    }
}
