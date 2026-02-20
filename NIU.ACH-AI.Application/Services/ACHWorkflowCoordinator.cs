using MassTransit;
using Microsoft.Extensions.Logging;
using NIU.ACH_AI.Application.Configuration;
using NIU.ACH_AI.Application.DTOs;
using NIU.ACH_AI.Application.Interfaces;
using NIU.ACH_AI.Application.Messaging.Events;

namespace NIU.ACH_AI.Application.Services
{
    /// <summary>
    /// Coordinates the execution of ACH workflow steps using the Saga Orchestrator.
    /// Publishes the start event.
    /// </summary>
    public class ACHWorkflowCoordinator : IACHWorkflowCoordinator
    {
        private readonly IExperimentInitializationService _initializationService;
        private readonly IExperimentMonitoringService _monitoringService;
        private readonly IPublishEndpoint _publishEndpoint;
        private readonly ILogger<ACHWorkflowCoordinator> _logger;

        public ACHWorkflowCoordinator(
            IExperimentInitializationService initializationService,
            IExperimentMonitoringService monitoringService,
            IPublishEndpoint publishEndpoint,
            ILoggerFactory loggerFactory)
        {
            ArgumentNullException.ThrowIfNull(initializationService);
            ArgumentNullException.ThrowIfNull(monitoringService);
            ArgumentNullException.ThrowIfNull(publishEndpoint);
            ArgumentNullException.ThrowIfNull(loggerFactory);

            _initializationService = initializationService;
            _monitoringService = monitoringService;
            _publishEndpoint = publishEndpoint;
            _logger = loggerFactory.CreateLogger<ACHWorkflowCoordinator>();
        }

        /// <summary>
        /// Executes the complete ACH workflow for the given experiment configuration.
        /// </summary>
        public async Task<ACHWorkflowResult> ExecuteWorkflowAsync(
            ExperimentConfiguration experimentConfig,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation($"Starting ACH workflow execution for experiment: {experimentConfig.Name}");

            try
            {
                var experimentId = await _initializationService.InitializeExperimentAsync(experimentConfig, cancellationToken);
                
                experimentConfig.Id = experimentId.ToString();

                _logger.LogInformation("Publishing IExperimentStarted event to start Saga.");
                await _publishEndpoint.Publish<IExperimentStarted>(new
                {
                    ExperimentId = experimentId,
                    Configuration = experimentConfig,
                    Timestamp = DateTime.UtcNow
                }, cancellationToken);

                return await _monitoringService.WaitForCompletionAsync(experimentId, experimentConfig.Name, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error executing ACH workflow for experiment: {experimentConfig.Name}");
                return new ACHWorkflowResult
                {
                    ExperimentId = experimentConfig.Id ?? string.Empty,
                    ExperimentName = experimentConfig.Name,
                    Success = false,
                    ErrorMessage = ex.Message
                };
            }
        }
    }
}
