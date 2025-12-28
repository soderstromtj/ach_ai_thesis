using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NIU.ACH_AI.Application.Configuration;
using NIU.ACH_AI.Application.DTOs;

namespace NIU.ACH_AI.Application.Interfaces
{
    /// <summary>
    /// Interface for orchestration execution service.
    /// </summary>
    public interface IOrchestrationExecutor
    {
        /// <summary>
        /// Executes an orchestration factory with the provided input.
        /// </summary>
        Task<TResult> ExecuteAsync<TResult>(
            IOrchestrationFactory<TResult> factory,
            OrchestrationPromptInput input,
            StepExecutionContext? stepExecutionContext = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Creates an AgentService for the specified ACH step configuration.
        /// </summary>
        IAgentService CreateAgentService(ACHStepConfiguration stepConfiguration);

        /// <summary>
        /// Gets the kernel builder service.
        /// </summary>
        IKernelBuilderService GetKernelBuilderService();

        /// <summary>
        /// Gets the logger factory.
        /// </summary>
        ILoggerFactory GetLoggerFactory();

        /// <summary>
        /// Creates orchestration options from step configuration.
        /// </summary>
        IOptions<OrchestrationSettings> CreateOrchestrationOptions(ACHStepConfiguration stepConfiguration);
    }
}
