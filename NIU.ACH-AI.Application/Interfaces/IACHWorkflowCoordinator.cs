using NIU.ACH_AI.Application.Configuration;
using NIU.ACH_AI.Application.DTOs;

namespace NIU.ACH_AI.Application.Interfaces
{
    /// <summary>
    /// Interface for coordinating ACH workflow execution.
    /// </summary>
    public interface IACHWorkflowCoordinator
    {
        /// <summary>
        /// Executes the complete ACH workflow for the given experiment configuration.
        /// </summary>
        /// <param name="experimentConfig">The experiment configuration</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The workflow execution result</returns>
        Task<ACHWorkflowResult> ExecuteWorkflowAsync(
            ExperimentConfiguration experimentConfig,
            CancellationToken cancellationToken = default);
    }
}
