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
    /// Manages the process of refining initial hypotheses based on findings.
    /// </summary>
    public class HypothesisRefinementConsumer : IConsumer<IHypothesisRefinementRequested>
    {
        private readonly IOrchestrationExecutor _orchestrationExecutor;
        private readonly IOrchestrationFactoryProvider _factoryProvider;
        private readonly IWorkflowPersistence _workflowPersistence;
        private readonly IWorkflowResultPersistence _workflowResultPersistence;
        private readonly ILogger<HypothesisRefinementConsumer> _logger;

        public HypothesisRefinementConsumer(
            IOrchestrationExecutor orchestrationExecutor,
            IOrchestrationFactoryProvider factoryProvider,
            IWorkflowPersistence workflowPersistence,
            IWorkflowResultPersistence workflowResultPersistence,
            ILogger<HypothesisRefinementConsumer> logger)
        {
            _orchestrationExecutor = orchestrationExecutor;
            _factoryProvider = factoryProvider;
            _workflowPersistence = workflowPersistence;
            _workflowResultPersistence = workflowResultPersistence;
            _logger = logger;
        }

        public async Task Consume(ConsumeContext<IHypothesisRefinementRequested> context)
        {
            var command = context.Message;
            _logger.LogInformation("Processing Refinement Request for Experiment {ExperimentId}", command.ExperimentId);

            try
            {
                var createdStepContext = await _workflowPersistence.CreateStepExecutionAsync(
                    command.ExperimentId,
                    command.Configuration,
                    null,
                    context.CancellationToken);

                var stepExecutionContext = command.StepContext;
                stepExecutionContext.StepExecutionId = createdStepContext.StepExecutionId;
                _logger.LogInformation("Step Execution Created with ID: {StepExecutionId}", stepExecutionContext.StepExecutionId);

                var factory = _factoryProvider.CreateFactory<List<Hypothesis>>(command.Configuration);
                
                var hypotheses = await _orchestrationExecutor.ExecuteAsync(
                    factory,
                    command.Input,
                    stepExecutionContext,
                    context.CancellationToken);

                _logger.LogInformation("Execution finished. Generated {Count} refined hypotheses.", hypotheses.Count);

                var savedHypotheses = await _workflowResultPersistence.SaveHypothesesAsync(
                    stepExecutionContext.StepExecutionId,
                    hypotheses,
                    isRefined: true,
                    cancellationToken: context.CancellationToken);

                _logger.LogInformation("Persisted {Count} refined hypotheses to database.", savedHypotheses.Count);

                await _workflowPersistence.UpdateStepExecutionStatusAsync(
                    stepExecutionContext.StepExecutionId,
                    "Completed",
                    end: DateTime.UtcNow,
                    cancellationToken: context.CancellationToken);

                _logger.LogInformation("Updated StepExecution status to Completed.");

                var resultMessage = new
                {
                    command.ExperimentId,
                    StepExecutionId = stepExecutionContext.StepExecutionId,
                    RefinedHypotheses = savedHypotheses,
                    Success = true
                };

                await context.Publish<IHypothesisRefinementResult>(resultMessage);
                // REMOVED: await context.RespondAsync<IHypothesisRefinementResult>(resultMessage);

                _logger.LogInformation("Refinement completed. Generated {Count} hypotheses.", savedHypotheses.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing refinement request");
                
                // REMOVED: await context.RespondAsync<IHypothesisRefinementResult>(...);
            }
        }
    }
}
