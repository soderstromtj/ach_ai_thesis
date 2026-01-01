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
                // Use the StepExecutionContext passed directly from the Coordinator.
                // This contains all necessary IDs (Experiment, Step, AgentConfigs) to execute the step
                // without needing to reload from the database.
                var stepExecutionContext = command.StepContext;

                var factory = _factoryProvider.CreateFactory<List<Hypothesis>>(command.Configuration);
                
                var hypotheses = await _orchestrationExecutor.ExecuteAsync(
                    factory,
                    command.Input,
                    stepExecutionContext,
                    context.CancellationToken);

                // We also need to save the results to the specialized table (Hypotheses table)
                // The Coordinator used to do this. We should do it here or let the Coordinator do it?
                // The principle of the Worker is to do the unit of work.
                // Saving the RESULT of the unit of work seems appropriate here.
                
                var savedHypotheses = await _workflowResultPersistence.SaveHypothesesAsync(
                    command.StepExecutionId,
                    hypotheses,
                    isRefined: false, // Brainstorming is usually initial
                    cancellationToken: context.CancellationToken);

                await context.RespondAsync<IBrainstormingResult>(new
                {
                    command.ExperimentId,
                    command.StepExecutionId,
                    Hypotheses = savedHypotheses,
                    Success = true
                });

                _logger.LogInformation("Brainstorming completed successfully. Generated {Count} hypotheses.", savedHypotheses.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing brainstorming request");
                
                await context.RespondAsync<IBrainstormingResult>(new
                {
                    command.ExperimentId,
                    command.StepExecutionId,
                    Hypotheses = new List<Hypothesis>(),
                    Success = false,
                    ErrorMessage = ex.Message
                });
            }
        }
    }
}
