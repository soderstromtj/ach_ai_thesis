using NIU.ACH_AI.Domain.Entities;

namespace NIU.ACH_AI.Infrastructure.Persistence.Repositories
{
    /// <summary>
    /// Repository interface for Hypothesis persistence operations
    /// </summary>
    public interface IHypothesisRepository
    {
        /// <summary>
        /// Saves a batch of hypotheses to the database
        /// </summary>
        /// <param name="hypotheses">The hypotheses to save</param>
        /// <param name="stepExecutionId">The step execution ID that generated these hypotheses</param>
        /// <param name="isRefined">Whether these hypotheses have been refined</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Number of hypotheses saved</returns>
        Task<int> SaveBatchAsync(
            IEnumerable<Hypothesis> hypotheses,
            Guid stepExecutionId,
            bool isRefined,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets all hypotheses for a specific step execution
        /// </summary>
        /// <param name="stepExecutionId">The step execution ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of hypotheses</returns>
        Task<IEnumerable<Hypothesis>> GetByStepExecutionIdAsync(
            Guid stepExecutionId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets all refined hypotheses for a specific step execution
        /// </summary>
        /// <param name="stepExecutionId">The step execution ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of refined hypotheses</returns>
        Task<IEnumerable<Hypothesis>> GetRefinedByStepExecutionIdAsync(
            Guid stepExecutionId,
            CancellationToken cancellationToken = default);
    }
}
