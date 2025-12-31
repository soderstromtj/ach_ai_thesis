using NIU.ACH_AI.Domain.Entities;

namespace NIU.ACH_AI.Application.Interfaces
{
    /// <summary>
    /// Defines the contract for persisting the results of workflow steps.
    /// </summary>
    public interface IWorkflowResultPersistence
    {
        /// <summary>
        /// Persists a collection of hypotheses.
        /// </summary>
        /// <param name="stepExecutionId">The ID of the step execution that produced the hypotheses.</param>
        /// <param name="hypotheses">The collection of hypotheses to save.</param>
        /// <param name="isRefined">Indicates whether these are initial or refined hypotheses.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The list of persisted hypotheses with updated IDs.</returns>
        Task<List<Hypothesis>> SaveHypothesesAsync(
            Guid stepExecutionId,
            IEnumerable<Hypothesis> hypotheses,
            bool isRefined,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Persists a collection of evidence.
        /// </summary>
        /// <param name="stepExecutionId">The ID of the step execution that extracted the evidence.</param>
        /// <param name="evidence">The collection of evidence to save.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The list of persisted evidence items with updated IDs.</returns>
        Task<List<Evidence>> SaveEvidenceAsync(
            Guid stepExecutionId,
            IEnumerable<Evidence> evidence,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Persists a collection of evidence-hypothesis evaluations.
        /// </summary>
        /// <param name="stepExecutionId">The ID of the current evaluation step execution.</param>
        /// <param name="evaluations">The collection of evaluations to save.</param>
        /// <param name="hypothesisStepExecutionId">The ID of the step execution that produced the hypotheses being evaluated.</param>
        /// <param name="evidenceStepExecutionId">The ID of the step execution that produced the evidence being evaluated.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task SaveEvaluationsAsync(
            Guid stepExecutionId,
            IEnumerable<EvidenceHypothesisEvaluation> evaluations,
            Guid hypothesisStepExecutionId,
            Guid evidenceStepExecutionId,
            CancellationToken cancellationToken = default);
    }
}
