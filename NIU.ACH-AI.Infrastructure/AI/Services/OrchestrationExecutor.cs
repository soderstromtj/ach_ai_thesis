using System.Net.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NIU.ACH_AI.Application.Configuration;
using NIU.ACH_AI.Application.DTOs;
using NIU.ACH_AI.Application.Interfaces;
using NIU.ACH_AI.Infrastructure.Configuration;

namespace NIU.ACH_AI.Infrastructure.AI.Services
{
    /// <summary>
    /// Runs orchestration factories and manages service resolution.
    /// </summary>
    public class OrchestrationExecutor : IOrchestrationExecutor
    {
        private readonly ILoggerFactory _loggerFactory;
        private readonly AIServiceSettings _aiServiceSettings;
        private readonly IKernelBuilderService _kernelBuilderService;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IAgentConfigurationPersistence _agentConfigurationPersistence;
        private readonly ILogger<OrchestrationExecutor> _logger;

        /// <summary>
        /// Sets up the orchestration executor.
        /// </summary>
        /// <param name="loggerFactory">Logger factory.</param>
        /// <param name="aiServiceSettings">AI service settings options.</param>
        /// <param name="kernelBuilderService">Kernel builder service.</param>
        /// <param name="httpClientFactory">HTTP client factory.</param>
        /// <param name="agentConfigurationPersistence">Persistence service for agent configurations.</param>
        public OrchestrationExecutor(
            ILoggerFactory loggerFactory,
            IOptions<AIServiceSettings> aiServiceSettings,
            IKernelBuilderService kernelBuilderService,
            IHttpClientFactory httpClientFactory,
            IAgentConfigurationPersistence agentConfigurationPersistence)
        {
            // Throw ArgumentNullException if any dependency is null
            ArgumentNullException.ThrowIfNull(loggerFactory, nameof(loggerFactory));
            ArgumentNullException.ThrowIfNull(aiServiceSettings, nameof(aiServiceSettings));
            ArgumentNullException.ThrowIfNull(kernelBuilderService, nameof(kernelBuilderService));
            ArgumentNullException.ThrowIfNull(httpClientFactory, nameof(httpClientFactory));
            ArgumentNullException.ThrowIfNull(agentConfigurationPersistence, nameof(agentConfigurationPersistence));

            _loggerFactory = loggerFactory;
            _aiServiceSettings = aiServiceSettings.Value;
            _kernelBuilderService = kernelBuilderService;
            _httpClientFactory = httpClientFactory;
            _agentConfigurationPersistence = agentConfigurationPersistence;
            _logger = loggerFactory.CreateLogger<OrchestrationExecutor>();
        }

        // ... ExecuteAsync ...

        /// <summary>
        /// Runs an orchestration factory with the provided configuration and input.
        /// </summary>
        /// <typeparam name="TResult">The result type returned by the factory.</typeparam>
        /// <param name="factory">The orchestration factory to execute.</param>
        /// <param name="input">The input for the orchestration.</param>
        /// <param name="stepExecutionContext">The execution context.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The result of the execution.</returns>
        public async Task<TResult> ExecuteAsync<TResult>(
            IOrchestrationFactory<TResult> factory,
            OrchestrationPromptInput input,
            StepExecutionContext? stepExecutionContext = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation($"Executing orchestration factory: {factory.GetType().Name}");
                var result = await factory.ExecuteCoreAsync(input, stepExecutionContext, cancellationToken);
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
        /// Builds an AgentService for the given ACH step configuration.
        /// </summary>
        public IAgentService CreateAgentService(ACHStepConfiguration stepConfiguration)
        {
            return new AgentService(
                stepConfiguration.AgentConfigurations,
                _aiServiceSettings,
                _loggerFactory,
                _httpClientFactory,
                _agentConfigurationPersistence);
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
        /// Builds orchestration options from a step configuration.
        /// </summary>
        public IOptions<OrchestrationSettings> CreateOrchestrationOptions(ACHStepConfiguration stepConfiguration)
        {
            return Options.Create(stepConfiguration.OrchestrationSettings);
        }
    }
}
