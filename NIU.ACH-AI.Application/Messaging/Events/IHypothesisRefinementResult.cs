using NIU.ACH_AI.Domain.Entities;

namespace NIU.ACH_AI.Application.Messaging.Events
{
    /// <summary>
    /// Result event for the Hypothesis Refinement step.
    /// </summary>
    public interface IHypothesisRefinementResult
    {
         Guid ExperimentId { get; }
         Guid StepExecutionId { get; }
         List<Hypothesis> RefinedHypotheses { get; }
         bool Success { get; }
         string? ErrorMessage { get; }
    }
}
