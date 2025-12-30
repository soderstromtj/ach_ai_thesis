using NIU.ACH_AI.Domain.Entities;

namespace NIU.ACH_AI.Application.Interfaces
{
    public interface IWorkflowResultPersistence
    {
        Task<List<Hypothesis>> SaveHypothesesAsync(
            Guid stepExecutionId,
            IEnumerable<Hypothesis> hypotheses,
            bool isRefined,
            CancellationToken cancellationToken = default);

        Task<List<Evidence>> SaveEvidenceAsync(
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
