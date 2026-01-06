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
    /// Publishes the start event and polls for completion.
    /// </summary>
    public class ACHWorkflowCoordinator : IACHWorkflowCoordinator
    {
        private readonly IPublishEndpoint _publishEndpoint;
        private readonly IWorkflowPersistence _workflowPersistence;
        private readonly ILogger<ACHWorkflowCoordinator> _logger;

        public ACHWorkflowCoordinator(
            IPublishEndpoint publishEndpoint,
            IWorkflowPersistence workflowPersistence,
            ILoggerFactory loggerFactory)
        {
            ArgumentNullException.ThrowIfNull(publishEndpoint);
            ArgumentNullException.ThrowIfNull(workflowPersistence);
            ArgumentNullException.ThrowIfNull(loggerFactory);

            _publishEndpoint = publishEndpoint;
            _workflowPersistence = workflowPersistence;
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
                // Create minimal entities
                var scenarioId = await _workflowPersistence.CreateScenarioAsync(experimentConfig.Context, cancellationToken);
                var experimentId = await _workflowPersistence.CreateExperimentAsync(experimentConfig, scenarioId, cancellationToken);
                
                // IMPORTANT: In the new Saga flow, we set ExperimentId based on what we generated.
                // However, the Saga State Machine uses CorrelationId.
                // We should match Configuration.Id if preset, or use the newly generated one.
                // experimentConfig.Id usually comes from UI/Input but let's sync them.
                experimentConfig.Id = experimentId.ToString();

                // Publish Start Event
                _logger.LogInformation("Publishing IExperimentStarted event to start Saga.");
                await _publishEndpoint.Publish<IExperimentStarted>(new
                {
                    ExperimentId = experimentId,
                    Configuration = experimentConfig,
                    Timestamp = DateTime.UtcNow
                }, cancellationToken);

                // Poll for completion
                _logger.LogInformation("Waiting for Saga completion...");
                
                while (!cancellationToken.IsCancellationRequested)
                {
                    var result = await _workflowPersistence.GetSagaResultAsync(experimentId, cancellationToken);
                    if (result != null)
                    {
                        // Saga Completed or Failed with result
                        if (result.Success)
                        {
                            _logger.LogInformation($"Successfully completed ACH workflow for experiment: {experimentConfig.Name}");
                        }
                        else
                        {
                            _logger.LogError($"ACH workflow failed: {result.ErrorMessage}");
                        }
                        return result;
                    }

                    await Task.Delay(2000, cancellationToken);
                }

                // If cancelled
                return new ACHWorkflowResult 
                { 
                    ExperimentId = experimentId.ToString(), 
                    ExperimentName = experimentConfig.Name,
                    Success = false, 
                    ErrorMessage = "Detailed execution cancelled." 
                };
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
