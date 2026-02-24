using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NIU.ACH_AI.Application.DTOs;
using NIU.ACH_AI.Application.Interfaces;

namespace NIU.ACH_AI.Application.Services
{
    /// <summary>
    /// Watches the progress of an active experiment and waits for the entire workflow to finish.
    /// </summary>
    public class ExperimentMonitoringService : IExperimentMonitoringService
    {
        private readonly IWorkflowPersistence _workflowPersistence;
        private readonly ILogger<ExperimentMonitoringService> _logger;

        public ExperimentMonitoringService(
            IWorkflowPersistence workflowPersistence,
            ILoggerFactory loggerFactory)
        {
            ArgumentNullException.ThrowIfNull(workflowPersistence);
            ArgumentNullException.ThrowIfNull(loggerFactory);

            _workflowPersistence = workflowPersistence;
            _logger = loggerFactory.CreateLogger<ExperimentMonitoringService>();
        }

        public async Task<ACHWorkflowResult> WaitForCompletionAsync(
            Guid experimentId,
            string experimentName,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Waiting for Saga completion for experiment: {ExperimentId}", experimentId);

            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    var result = await _workflowPersistence.GetSagaResultAsync(experimentId, cancellationToken);
                    if (result != null)
                    {
                        if (result.Success)
                        {
                            _logger.LogInformation("Successfully completed ACH workflow for experiment: {ExperimentName}", experimentName);
                        }
                        else
                        {
                            _logger.LogError("ACH workflow failed for experiment '{ExperimentName}': {ErrorMessage}", experimentName, result.ErrorMessage);
                        }
                        return result;
                    }

                    await Task.Delay(2000, cancellationToken);
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("Waiting for Saga completion for experiment '{ExperimentName}' was cancelled.", experimentName);
            }

            return new ACHWorkflowResult
            {
                ExperimentId = experimentId.ToString(),
                ExperimentName = experimentName,
                Success = false,
                ErrorMessage = "Detailed execution cancelled."
            };
        }
    }
}
