using Microsoft.EntityFrameworkCore;
using NIU.ACH_AI.Application.Configuration;
using NIU.ACH_AI.Application.DTOs;
using NIU.ACH_AI.Application.Interfaces;
using DbModel = NIU.ACH_AI.Infrastructure.Persistence.Models;

namespace NIU.ACH_AI.Infrastructure.Persistence.Services
{
    /// <summary>
    /// EF Core-backed persistence for experiment and step execution metadata.
    /// Handles creation of scenarios, experiments, and step executions with
    /// minimal orchestration context passed from the application layer.
    /// </summary>
    public class WorkflowPersistence : IWorkflowPersistence
    {
        private readonly DbModel.AchAIDbContext _context;
        private readonly DbModel.ACHSagaDbContext _sagaContext;

        /// <summary>
        /// Creates a persistence service using the ACH AI database context.
        /// </summary>
        /// <param name="context">The ACH AI database context.</param>
        /// <param name="sagaContext">The ACH Saga database context.</param>
        public WorkflowPersistence(DbModel.AchAIDbContext context, DbModel.ACHSagaDbContext sagaContext)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _sagaContext = sagaContext ?? throw new ArgumentNullException(nameof(sagaContext));
        }

        /// <summary>
        /// Persists a scenario row containing the experiment context text.
        /// </summary>
        /// <param name="context">The ACH AI database context.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        public async Task<Guid> CreateScenarioAsync(string context, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(context))
            {
                throw new ArgumentException("Scenario context must be provided.", nameof(context));
            }

            var scenario = new DbModel.Scenario
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
        /// <param name="configuration">The experiment configuration.</param>
        /// <param name="scenarioId">The ID of the associated scenario.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
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

            var experiment = new DbModel.Experiment
            {
                ExperimentId = Guid.NewGuid(),
                ExperimentName = configuration.Name,
                Description = configuration.Description,
                KeyQuestion = configuration.KeyQuestion,
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
        /// <param name="experimentId">The ID of the associated experiment.</param>
        /// <param name="stepConfiguration">Configuration for the ACH step.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        public async Task<StepExecutionContext> CreateStepExecutionAsync(
            Guid experimentId,
            ACHStepConfiguration stepConfiguration,
            Guid? stepExecutionId = null,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(stepConfiguration, nameof(stepConfiguration));
            if (experimentId == Guid.Empty)
            {
                throw new ArgumentException("Experiment ID must be provided.", nameof(experimentId));
            }

            // check if step execution already exists if ID is provided
            if (stepExecutionId.HasValue)
            {
                 var existing = await _context.StepExecutions
                    .AsNoTracking()
                    .FirstOrDefaultAsync(s => s.StepExecutionId == stepExecutionId.Value, cancellationToken);
                 
                 if (existing != null)
                 {
                     return new StepExecutionContext
                     {
                         ExperimentId = existing.ExperimentId,
                         StepExecutionId = existing.StepExecutionId,
                         AchStepId = existing.AchStepId,
                         AchStepName = existing.AchStepName,
                         Status = existing.ExecutionStatus // Return status so consumer can check if complete
                     };
                 }
            }

            Guid? orchestrationTypeId = await ResolveOrchestrationTypeIdAsync(
                stepConfiguration.Name,
                cancellationToken);

            // Use provided ID or generate new
            var newId = stepExecutionId ?? Guid.NewGuid();

            var stepExecution = new DbModel.StepExecution
            {
                StepExecutionId = newId,
                ExperimentId = experimentId,
                AchStepId = stepConfiguration.Id,
                AchStepName = stepConfiguration.Name,
                Description = stepConfiguration.Description,
                TaskInstructions = stepConfiguration.TaskInstructions,
                OrchestrationTypeId = orchestrationTypeId,
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
                 // Handle race condition where another consumer insert it just now
                 var existing = await _context.StepExecutions
                    .AsNoTracking()
                    .FirstOrDefaultAsync(s => s.StepExecutionId == newId, cancellationToken);

                 if (existing != null)
                 {
                     return new StepExecutionContext
                     {
                         ExperimentId = existing.ExperimentId,
                         StepExecutionId = existing.StepExecutionId,
                         AchStepId = existing.AchStepId,
                         AchStepName = existing.AchStepName,
                         Status = existing.ExecutionStatus
                     };
                 }
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

        private async Task<Guid?> ResolveOrchestrationTypeIdAsync(
            string stepName,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(stepName))
            {
                return null;
            }

            var description = stepName.ToLowerInvariant() switch
            {
                "hypothesis refinement" or "hypothesisrefinement" or "hypothesis evaluation" or "hypothesisevaluation"
                    => "Sequential",
                _ => "Concurrent"
            };

            var orchestrationTypeId = await _context.OrchestrationTypes
                .AsNoTracking()
                .Where(t => t.Description.ToLower() == description.ToLower())
                .Select(t => t.OrchestrationTypeId)
                .FirstOrDefaultAsync(cancellationToken);

            return orchestrationTypeId == Guid.Empty ? null : orchestrationTypeId;
        }
        /// <summary>
        /// Retrieves the step execution context by ID.
        /// </summary>
        public async Task<StepExecutionContext?> GetStepExecutionAsync(Guid stepExecutionId, CancellationToken cancellationToken = default)
        {
            var step = await _context.StepExecutions
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.StepExecutionId == stepExecutionId, cancellationToken);

            if (step == null)
            {
                return null;
            }

            return new StepExecutionContext
            {
                ExperimentId = step.ExperimentId,
                StepExecutionId = step.StepExecutionId,
                AchStepId = step.AchStepId,
                AchStepName = step.AchStepName
            };
        }
        /// <summary>
        /// Retrieves the experimental result from the saga state.
        /// </summary>
        /// <param name="experimentId">The experiment ID.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The result or null if incomplete.</returns>
        public async Task<ACHWorkflowResult?> GetSagaResultAsync(Guid experimentId, CancellationToken cancellationToken = default)
        {
            // Query the Saga table
            var sagaState = await _sagaContext.Set<DbModel.ExperimentState>()
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.CorrelationId == experimentId, cancellationToken);
             
            if (sagaState == null)
            {
                return null;
            }

            // Check if completed
            // CurrentState acts as string in EF Core map
            // Assuming "Final" or "Completed" or "Failed"
            // MassTransit convention: "Final" state means instance is removed?
            // Unless configured to keep. We configured to Keep? 
            // Default: Completed instances are removed if Finalize is called.
            // In our StateMachine: .TransitionTo(Completed)
            // We defined Completed as a State. Not Final.
            
            if (sagaState.CurrentState != "Completed" && sagaState.CurrentState != "Failed")
            {
                return null;
            }

            // Deserialize result if available
            if (string.IsNullOrWhiteSpace(sagaState.SerializedResult))
            {
                // Can happen if Failed early?
                if (sagaState.CurrentState == "Failed")
                {
                     // Return a failed result wrapper
                     return new ACHWorkflowResult 
                     { 
                        Success = false, 
                        ErrorMessage = "Saga failed (No result details persisted)." 
                     };
                }
                return null; 
            }

            try
            {
                return System.Text.Json.JsonSerializer.Deserialize<ACHWorkflowResult>(sagaState.SerializedResult);
            }
            catch
            {
                // Fallback
                return null;
            }
        }
    }
}
