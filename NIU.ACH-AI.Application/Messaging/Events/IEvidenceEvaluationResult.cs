using NIU.ACH_AI.Domain.Entities;

namespace NIU.ACH_AI.Application.Messaging.Events
{
    /// <summary>
    /// Serves as the completion payload for the exhaustive scoring phase.
    /// </summary>
    /// <remarks>
    /// Broadcast to consolidate all individual pairing assessments into a final analytical matrix.
    /// </remarks>
    public interface IEvidenceEvaluationResult
    {
         Guid ExperimentId { get; }
         Guid StepExecutionId { get; }
         List<EvidenceHypothesisEvaluation> Evaluations { get; }
         bool Success { get; }
         string? ErrorMessage { get; }
    }
}
