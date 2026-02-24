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
    /// Manages the workflow for extracting factual evidence from the provided context.
    /// </summary>
    public class EvidenceExtractionConsumer : IConsumer<IEvidenceExtractionRequested>
    {
        private readonly IOrchestrationExecutor _orchestrationExecutor;
        private readonly IOrchestrationFactoryProvider _factoryProvider;
        private readonly IWorkflowPersistence _workflowPersistence;
        private readonly IWorkflowResultPersistence _workflowResultPersistence;
        private readonly ILogger<EvidenceExtractionConsumer> _logger;

        public EvidenceExtractionConsumer(
            IOrchestrationExecutor orchestrationExecutor,
            IOrchestrationFactoryProvider factoryProvider,
            IWorkflowPersistence workflowPersistence,
            IWorkflowResultPersistence workflowResultPersistence,
            ILogger<EvidenceExtractionConsumer> logger)
        {
            _orchestrationExecutor = orchestrationExecutor;
            _factoryProvider = factoryProvider;
            _workflowPersistence = workflowPersistence;
            _workflowResultPersistence = workflowResultPersistence;
            _logger = logger;
        }

        public async Task Consume(ConsumeContext<IEvidenceExtractionRequested> context)
        {
            var command = context.Message;
            _logger.LogInformation("Processing Evidence Extraction Request for Experiment {ExperimentId}", command.ExperimentId);

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

                var factory = _factoryProvider.CreateFactory<List<Evidence>>(command.Configuration);
                
                var evidence = await _orchestrationExecutor.ExecuteAsync(
                    factory,
                    command.Input,
                    stepExecutionContext,
                    context.CancellationToken);
                
                _logger.LogInformation("Execution finished. Extracted {Count} evidence items.", evidence.Count);

                var savedEvidence = await _workflowResultPersistence.SaveEvidenceAsync(
                    stepExecutionContext.StepExecutionId,
                    evidence,
                    cancellationToken: context.CancellationToken);

                _logger.LogInformation("Persisted {Count} evidence items to database.", savedEvidence.Count);

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
                    Evidence = savedEvidence,
                    Success = true
                };

                await context.Publish<IEvidenceExtractionResult>(resultMessage);
                // REMOVED: await context.RespondAsync<IEvidenceExtractionResult>(resultMessage);

                _logger.LogInformation("Evidence Extraction completed. Extracted {Count} items.", savedEvidence.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing evidence extraction request");
                
                // REMOVED: await context.RespondAsync<IEvidenceExtractionResult>(...);
            }
        }
    }
}
