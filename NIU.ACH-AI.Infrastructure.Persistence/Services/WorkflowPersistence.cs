using Microsoft.EntityFrameworkCore;
using NIU.ACH_AI.Application.Configuration;
using NIU.ACH_AI.Application.DTOs;
using NIU.ACH_AI.Application.Interfaces;
using NIU.ACH_AI.Infrastructure.Persistence.Models;

namespace NIU.ACH_AI.Infrastructure.Persistence.Services
{
    /// <summary>
    /// EF Core-backed persistence for experiment and step execution metadata.
    /// Handles creation of scenarios, experiments, and step executions with
    /// minimal orchestration context passed from the application layer.
    /// </summary>
    public class WorkflowPersistence : IWorkflowPersistence
    {
        private readonly AchAIDbContext _context;

        /// <summary>
        /// Creates a persistence service using the ACH AI database context.
        /// </summary>
        public WorkflowPersistence(AchAIDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        /// <summary>
        /// Persists a scenario row containing the experiment context text.
        /// </summary>
        public async Task<Guid> CreateScenarioAsync(string context, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(context))
            {
                throw new ArgumentException("Scenario context must be provided.", nameof(context));
            }

            var scenario = new Scenario
            {
                ScenarioId = Guid.NewGuid(),
                Context = context
            };

            try
            {
                _context.Scenarios.Add(scenario);
                await _context.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException ex)
            {
                throw new InvalidOperationException("Failed to persist scenario.", ex);
            }

            return scenario.ScenarioId;
        }

        /// <summary>
        /// Persists an experiment row associated to the provided scenario.
        /// </summary>
        public async Task<Guid> CreateExperimentAsync(
            ExperimentConfiguration configuration,
            Guid scenarioId,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(configuration, nameof(configuration));
            if (scenarioId == Guid.Empty)
            {
                throw new ArgumentException("Scenario ID must be provided.", nameof(scenarioId));
            }

            var experiment = new Experiment
            {
                ExperimentId = Guid.NewGuid(),
                ExperimentName = configuration.Name,
                Description = configuration.Description,
                Kiq = configuration.KeyQuestion,
                ScenarioId = scenarioId,
                CreatedAt = DateTime.UtcNow
            };

            try
            {
                _context.Experiments.Add(experiment);
                await _context.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException ex)
            {
                throw new InvalidOperationException("Failed to persist experiment.", ex);
            }

            return experiment.ExperimentId;
        }

        /// <summary>
        /// Persists a step execution row for a specific ACH step.
        /// Returns a context object for downstream orchestration.
        /// </summary>
        public async Task<StepExecutionContext> CreateStepExecutionAsync(
            Guid experimentId,
            ACHStepConfiguration stepConfiguration,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(stepConfiguration, nameof(stepConfiguration));
            if (experimentId == Guid.Empty)
            {
                throw new ArgumentException("Experiment ID must be provided.", nameof(experimentId));
            }

            var stepExecution = new StepExecution
            {
                StepExecutionId = Guid.NewGuid(),
                ExperimentId = experimentId,
                AchStepId = stepConfiguration.Id,
                AchStepName = stepConfiguration.Name,
                Description = stepConfiguration.Description,
                TaskInstructions = stepConfiguration.TaskInstructions,
                ExecutionStatus = "NotStarted",
                RetryCount = 0
            };

            try
            {
                _context.StepExecutions.Add(stepExecution);
                await _context.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException ex)
            {
                throw new InvalidOperationException("Failed to persist step execution.", ex);
            }

            return new StepExecutionContext
            {
                ExperimentId = experimentId,
                StepExecutionId = stepExecution.StepExecutionId,
                AchStepId = stepExecution.AchStepId,
                AchStepName = stepExecution.AchStepName
            };
        }

        /// <summary>
        /// Updates the execution status and optional timing/error fields for a step.
        /// </summary>
        public async Task UpdateStepExecutionStatusAsync(
            Guid stepExecutionId,
            string status,
            DateTime? start = null,
            DateTime? end = null,
            string? errorType = null,
            string? errorMessage = null,
            int? retryCount = null,
            CancellationToken cancellationToken = default)
        {
            if (stepExecutionId == Guid.Empty)
            {
                throw new ArgumentException("Step execution ID must be provided.", nameof(stepExecutionId));
            }

            if (string.IsNullOrWhiteSpace(status))
            {
                throw new ArgumentException("Status must be provided.", nameof(status));
            }

            var stepExecution = await _context.StepExecutions
                .FirstOrDefaultAsync(s => s.StepExecutionId == stepExecutionId, cancellationToken);

            if (stepExecution == null)
            {
                throw new InvalidOperationException(
                    $"StepExecution '{stepExecutionId}' not found.");
            }

            stepExecution.ExecutionStatus = status;

            if (start.HasValue)
            {
                stepExecution.DatetimeStart = start.Value;
            }

            if (end.HasValue)
            {
                stepExecution.DatetimeEnd = end.Value;
            }

            if (!string.IsNullOrWhiteSpace(errorType))
            {
                stepExecution.ErrorType = errorType;
            }

            if (!string.IsNullOrWhiteSpace(errorMessage))
            {
                stepExecution.ErrorMessage = errorMessage;
            }

            if (retryCount.HasValue)
            {
                stepExecution.RetryCount = retryCount.Value;
            }

            try
            {
                await _context.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException ex)
            {
                throw new InvalidOperationException("Failed to update step execution status.", ex);
            }
        }
    }
}
