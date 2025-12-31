using NIU.ACH_AI.Application.Configuration;
using NIU.ACH_AI.Application.DTOs;

namespace NIU.ACH_AI.Application.Interfaces
{
    /// <summary>
    /// Defines the contract for persisting workflow execution state and metadata.
    /// </summary>
    public interface IWorkflowPersistence
    {
        /// <summary>
        /// Creates a new scenario record with the given context.
        /// </summary>
        /// <param name="context">The background context or problem statement.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The unique identifier of the created scenario.</returns>
        Task<Guid> CreateScenarioAsync(string context, CancellationToken cancellationToken = default);

        /// <summary>
        /// Creates a new experiment record linked to a scenario.
        /// </summary>
        /// <param name="configuration">The experiment configuration.</param>
        /// <param name="scenarioId">The ID of the parent scenario.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The unique identifier of the created experiment.</returns>
        Task<Guid> CreateExperimentAsync(
            ExperimentConfiguration configuration,
            Guid scenarioId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Creates a new step execution record.
        /// </summary>
        /// <param name="experimentId">The ID of the parent experiment.</param>
        /// <param name="stepConfiguration">The configuration for the step being executed.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The context object for the created step execution.</returns>
        Task<StepExecutionContext> CreateStepExecutionAsync(
            Guid experimentId,
            ACHStepConfiguration stepConfiguration,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Updates the status and details of an existing step execution.
        /// </summary>
        /// <param name="stepExecutionId">The ID of the step execution to update.</param>
        /// <param name="status">The new status (e.g., "Completed", "Failed").</param>
        /// <param name="start">The start timestamp (optional).</param>
        /// <param name="end">The end timestamp (optional).</param>
        /// <param name="errorType">The type of error if failed (optional).</param>
        /// <param name="errorMessage">The error message if failed (optional).</param>
        /// <param name="retryCount">The number of retries attempted (optional).</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task UpdateStepExecutionStatusAsync(
            Guid stepExecutionId,
            string status,
            DateTime? start = null,
            DateTime? end = null,
            string? errorType = null,
            string? errorMessage = null,
            int? retryCount = null,
            CancellationToken cancellationToken = default);
    }
}
