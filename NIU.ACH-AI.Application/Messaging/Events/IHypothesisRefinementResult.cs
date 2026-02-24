using NIU.ACH_AI.Domain.Entities;

namespace NIU.ACH_AI.Application.Messaging.Events
{
    /// <summary>
    /// Contains the output from the hypothesis refinement process.
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
