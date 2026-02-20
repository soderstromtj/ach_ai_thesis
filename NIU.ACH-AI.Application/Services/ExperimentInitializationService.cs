using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NIU.ACH_AI.Application.Configuration;
using NIU.ACH_AI.Application.Interfaces;

namespace NIU.ACH_AI.Application.Services
{
    public class ExperimentInitializationService : IExperimentInitializationService
    {
        private readonly IWorkflowPersistence _workflowPersistence;
        private readonly ILogger<ExperimentInitializationService> _logger;

        public ExperimentInitializationService(
            IWorkflowPersistence workflowPersistence,
            ILoggerFactory loggerFactory)
        {
            ArgumentNullException.ThrowIfNull(workflowPersistence);
            ArgumentNullException.ThrowIfNull(loggerFactory);

            _workflowPersistence = workflowPersistence;
            _logger = loggerFactory.CreateLogger<ExperimentInitializationService>();
        }

        public async Task<Guid> InitializeExperimentAsync(
            ExperimentConfiguration experimentConfig,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Initializing scenario and experiment entities for: {ExperimentName}", experimentConfig.Name);

            var scenarioId = await _workflowPersistence.CreateScenarioAsync(experimentConfig.Context, cancellationToken);
            var experimentId = await _workflowPersistence.CreateExperimentAsync(experimentConfig, scenarioId, cancellationToken);

            return experimentId;
        }
    }
}
