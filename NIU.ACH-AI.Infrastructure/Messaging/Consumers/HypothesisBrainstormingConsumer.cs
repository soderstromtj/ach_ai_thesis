using MassTransit;
using Microsoft.Extensions.Logging;
using NIU.ACH_AI.Application.DTOs;
using NIU.ACH_AI.Application.Interfaces;
using NIU.ACH_AI.Application.Messaging.Commands;
using NIU.ACH_AI.Application.Messaging.Events;
using NIU.ACH_AI.Domain.Entities;

namespace NIU.ACH_AI.Infrastructure.Messaging.Consumers
{
    /// <summary>
    /// Consumer that handles Hypothesis Brainstorming requests.
    /// Orchestrates the brainstorming process and returns the results.
    /// </summary>
    public class HypothesisBrainstormingConsumer : IConsumer<IBrainstormingRequested>
    {
        private readonly IOrchestrationExecutor _orchestrationExecutor;
        private readonly IOrchestrationFactoryProvider _factoryProvider;
        private readonly IWorkflowPersistence _workflowPersistence;
        private readonly IWorkflowResultPersistence _workflowResultPersistence;
        private readonly ILogger<HypothesisBrainstormingConsumer> _logger;

        public HypothesisBrainstormingConsumer(
            IOrchestrationExecutor orchestrationExecutor,
            IOrchestrationFactoryProvider factoryProvider,
            IWorkflowPersistence workflowPersistence,
            IWorkflowResultPersistence workflowResultPersistence,
            ILogger<HypothesisBrainstormingConsumer> logger)
        {
            _orchestrationExecutor = orchestrationExecutor;
            _factoryProvider = factoryProvider;
            _workflowPersistence = workflowPersistence;
            _workflowResultPersistence = workflowResultPersistence;
            _logger = logger;
        }

        public async Task Consume(ConsumeContext<IBrainstormingRequested> context)
        {
            var command = context.Message;
            _logger.LogInformation("Processing Brainstorming Request for Experiment {ExperimentId}, Step {StepExecutionId}", 
                command.ExperimentId, command.StepExecutionId);

            try
            {
                // Create the StepExecution record
                // We use the ID returned by CreateStepExecutionAsync and update the command.StepContext with it.
                // This effectively overrides the Saga's pre-generated ID with the one actually persisted.
                var createdStepContext = await _workflowPersistence.CreateStepExecutionAsync(
                    command.ExperimentId,
                    command.Configuration,
                    context.CancellationToken);

                // Use the persisted ID for subsequent operations
                var stepExecutionContext = command.StepContext;
                stepExecutionContext.StepExecutionId = createdStepContext.StepExecutionId;

                var factory = _factoryProvider.CreateFactory<List<Hypothesis>>(command.Configuration);
                
                var hypotheses = await _orchestrationExecutor.ExecuteAsync(
                    factory,
                    command.Input,
                    stepExecutionContext,
                    context.CancellationToken);

                // We also need to save the results
                var savedHypotheses = await _workflowResultPersistence.SaveHypothesesAsync(
                    stepExecutionContext.StepExecutionId,
                    hypotheses,
                    isRefined: false, 
                    cancellationToken: context.CancellationToken);

                await _workflowPersistence.UpdateStepExecutionStatusAsync(
                    stepExecutionContext.StepExecutionId,
                    "Completed",
                    end: DateTime.UtcNow,
                    cancellationToken: context.CancellationToken);

                var resultMessage = new
                {
                    command.ExperimentId,
                    StepExecutionId = stepExecutionContext.StepExecutionId, // Return the valid one
                    Hypotheses = savedHypotheses,
                    Success = true
                };

                // Publish as Event for Saga
                await context.Publish<IBrainstormingResult>(resultMessage);

                // Respond for RequestClient (Backward Compatibility)
                // Respond for RequestClient (Backward Compatibility)
                // REMOVED: await context.RespondAsync<IBrainstormingResult>(resultMessage);

                _logger.LogInformation("Brainstorming completed successfully. Generated {Count} hypotheses.", savedHypotheses.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing brainstorming request");
                
                var errorMessage = ex.InnerException != null 
                    ? $"{ex.Message} Inner: {ex.InnerException.Message}" 
                    : ex.Message;

                // REMOVED: await context.RespondAsync<IBrainstormingResult>(...)
            }
        }
    }
}
