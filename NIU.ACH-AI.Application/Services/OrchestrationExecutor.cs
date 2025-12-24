using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NIU.ACH_AI.Application.Configuration;
using NIU.ACH_AI.Application.DTOs;
using NIU.ACH_AI.Application.Interfaces;

namespace NIU.ACH_AI.Application.Services
{
    /// <summary>
    /// Service responsible for executing orchestration factories.
    /// Encapsulates the common pattern of service resolution and factory execution.
    /// </summary>
    public class OrchestrationExecutor : IOrchestrationExecutor
    {
        private readonly ILoggerFactory _loggerFactory;
        private readonly AIServiceSettings _aiServiceSettings;
        private readonly IKernelBuilderService _kernelBuilderService;
        private readonly ILogger<OrchestrationExecutor> _logger;

        public OrchestrationExecutor(
            ILoggerFactory loggerFactory,
            IOptions<AIServiceSettings> aiServiceSettings,
            IKernelBuilderService kernelBuilderService)
        {
            _loggerFactory = loggerFactory;
            _aiServiceSettings = aiServiceSettings.Value;
            _kernelBuilderService = kernelBuilderService;
            _logger = loggerFactory.CreateLogger<OrchestrationExecutor>();
        }

        /// <summary>
        /// Executes an orchestration factory with the provided configuration and input.
        /// </summary>
        /// <typeparam name="TResult">The result type returned by the factory</typeparam>
        /// <param name="factory">The orchestration factory to execute</param>
        /// <param name="input">The input for the orchestration</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The result of the orchestration execution</returns>
        public async Task<TResult> ExecuteAsync<TResult>(
            IOrchestrationFactory<TResult> factory,
            OrchestrationPromptInput input,
            CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation($"Executing orchestration factory: {factory.GetType().Name}");
                var result = await factory.ExecuteCoreAsync(input, cancellationToken);
                _logger.LogInformation($"Successfully completed orchestration factory: {factory.GetType().Name}");
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error executing orchestration factory: {factory.GetType().Name}");
                throw;
            }
        }

        /// <summary>
        /// Creates an AgentService for the specified ACH step configuration.
        /// </summary>
        public IAgentService CreateAgentService(ACHStepConfiguration stepConfiguration)
        {
            return new Infrastructure.AI.Services.AgentService(
                stepConfiguration.AgentConfigurations,
                _aiServiceSettings,
                _loggerFactory);
        }

        /// <summary>
        /// Gets the kernel builder service.
        /// </summary>
        public IKernelBuilderService GetKernelBuilderService()
        {
            return _kernelBuilderService;
        }

        /// <summary>
        /// Gets the logger factory.
        /// </summary>
        public ILoggerFactory GetLoggerFactory()
        {
            return _loggerFactory;
        }

        /// <summary>
        /// Creates orchestration options from step configuration.
        /// </summary>
        public IOptions<OrchestrationSettings> CreateOrchestrationOptions(ACHStepConfiguration stepConfiguration)
        {
            return Options.Create(stepConfiguration.OrchestrationSettings);
        }
    }
}
