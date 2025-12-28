using NIU.ACH_AI.Domain.Entities;

namespace NIU.ACH_AI.Application.Interfaces
{
    public interface IWorkflowResultPersistence
    {
        Task SaveHypothesesAsync(
            Guid stepExecutionId,
            IEnumerable<Hypothesis> hypotheses,
            bool isRefined,
            CancellationToken cancellationToken = default);

        Task SaveEvidenceAsync(
            Guid stepExecutionId,
            IEnumerable<Evidence> evidence,
            CancellationToken cancellationToken = default);

        Task SaveEvaluationsAsync(
            Guid stepExecutionId,
            IEnumerable<EvidenceHypothesisEvaluation> evaluations,
            Guid hypothesisStepExecutionId,
            Guid evidenceStepExecutionId,
            CancellationToken cancellationToken = default);
    }
}
