using NIU.ACH_AI.Domain.Entities;

namespace NIU.ACH_AI.Application.Messaging.Events
{
    /// <summary>
    /// Contains the results after evaluating a single piece of evidence against a single hypothesis.
    /// </summary>
    public interface IPairEvaluated
    {
        Guid ExperimentId { get; }
        Guid StepExecutionId { get; }
        EvidenceHypothesisEvaluation Evaluation { get; }
        bool Success { get; }
        string ErrorMessage { get; }
    }
}
