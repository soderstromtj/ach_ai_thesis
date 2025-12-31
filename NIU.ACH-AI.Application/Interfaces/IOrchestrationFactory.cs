using NIU.ACH_AI.Application.DTOs;

namespace NIU.ACH_AI.Application.Interfaces
{
    /// <summary>
    /// Defines the contract for an orchestration factory that executes a workflow step.
    /// </summary>
    /// <typeparam name="TResult">The type of the result produced by the orchestration.</typeparam>
    public interface IOrchestrationFactory<TResult>
    {
        /// <summary>
        /// Executes the core orchestration logic for the step.
        /// </summary>
        /// <param name="input">The input prompt and context for the orchestration.</param>
        /// <param name="stepExecutionContext">The context for the current step execution.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The result of the orchestration execution.</returns>
        Task<TResult> ExecuteCoreAsync(
            OrchestrationPromptInput input,
            StepExecutionContext? stepExecutionContext = null,
            CancellationToken cancellationToken = default);
    }
}
