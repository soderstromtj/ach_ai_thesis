using NIU.ACH_AI.Domain.Entities;

namespace NIU.ACH_AI.Application.Messaging.Events
{
    /// <summary>
    /// Result event for the Evidence-Hypothesis Evaluation step.
    /// </summary>
    public interface IEvidenceEvaluationResult
    {
         Guid ExperimentId { get; }
         Guid StepExecutionId { get; }
         List<EvidenceHypothesisEvaluation> Evaluations { get; }
         bool Success { get; }
         string? ErrorMessage { get; }
    }
}
