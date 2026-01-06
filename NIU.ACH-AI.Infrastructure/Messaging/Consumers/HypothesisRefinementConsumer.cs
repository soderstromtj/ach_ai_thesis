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
    /// Consumer that handles Hypothesis Refinement requests.
    /// </summary>
    public class HypothesisRefinementConsumer : IConsumer<IHypothesisRefinementRequested>
    {
        private readonly IOrchestrationExecutor _orchestrationExecutor;
        private readonly IOrchestrationFactoryProvider _factoryProvider;
        private readonly IWorkflowResultPersistence _workflowResultPersistence;
        private readonly ILogger<HypothesisRefinementConsumer> _logger;

        public HypothesisRefinementConsumer(
            IOrchestrationExecutor orchestrationExecutor,
            IOrchestrationFactoryProvider factoryProvider,
            IWorkflowResultPersistence workflowResultPersistence,
            ILogger<HypothesisRefinementConsumer> logger)
        {
            _orchestrationExecutor = orchestrationExecutor;
            _factoryProvider = factoryProvider;
            _workflowResultPersistence = workflowResultPersistence;
            _logger = logger;
        }

        public async Task Consume(ConsumeContext<IHypothesisRefinementRequested> context)
        {
            var command = context.Message;
            _logger.LogInformation("Processing Refinement Request for Experiment {ExperimentId}", command.ExperimentId);

            try
            {
                var stepExecutionContext = command.StepContext;
                var factory = _factoryProvider.CreateFactory<List<Hypothesis>>(command.Configuration);
                
                var hypotheses = await _orchestrationExecutor.ExecuteAsync(
                    factory,
                    command.Input,
                    stepExecutionContext,
                    context.CancellationToken);

                var savedHypotheses = await _workflowResultPersistence.SaveHypothesesAsync(
                    command.StepExecutionId,
                    hypotheses,
                    isRefined: true,
                    cancellationToken: context.CancellationToken);

                var resultMessage = new
                {
                    command.ExperimentId,
                    command.StepExecutionId,
                    RefinedHypotheses = savedHypotheses,
                    Success = true
                };

                await context.Publish<IHypothesisRefinementResult>(resultMessage);
                await context.RespondAsync<IHypothesisRefinementResult>(resultMessage);

                _logger.LogInformation("Refinement completed. Generated {Count} hypotheses.", savedHypotheses.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing refinement request");
                
                await context.RespondAsync<IHypothesisRefinementResult>(new
                {
                    command.ExperimentId,
                    command.StepExecutionId,
                    RefinedHypotheses = new List<Hypothesis>(),
                    Success = false,
                    ErrorMessage = ex.Message
                });
            }
        }
    }
}
