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
    /// Consumer that handles Evidence Extraction requests.
    /// </summary>
    public class EvidenceExtractionConsumer : IConsumer<IEvidenceExtractionRequested>
    {
        private readonly IOrchestrationExecutor _orchestrationExecutor;
        private readonly IOrchestrationFactoryProvider _factoryProvider;
        private readonly IWorkflowResultPersistence _workflowResultPersistence;
        private readonly ILogger<EvidenceExtractionConsumer> _logger;

        public EvidenceExtractionConsumer(
            IOrchestrationExecutor orchestrationExecutor,
            IOrchestrationFactoryProvider factoryProvider,
            IWorkflowResultPersistence workflowResultPersistence,
            ILogger<EvidenceExtractionConsumer> logger)
        {
            _orchestrationExecutor = orchestrationExecutor;
            _factoryProvider = factoryProvider;
            _workflowResultPersistence = workflowResultPersistence;
            _logger = logger;
        }

        public async Task Consume(ConsumeContext<IEvidenceExtractionRequested> context)
        {
            var command = context.Message;
            _logger.LogInformation("Processing Evidence Extraction Request for Experiment {ExperimentId}", command.ExperimentId);

            try
            {
                var stepExecutionContext = command.StepContext;
                var factory = _factoryProvider.CreateFactory<List<Evidence>>(command.Configuration);
                
                var evidence = await _orchestrationExecutor.ExecuteAsync(
                    factory,
                    command.Input,
                    stepExecutionContext,
                    context.CancellationToken);

                var savedEvidence = await _workflowResultPersistence.SaveEvidenceAsync(
                    command.StepExecutionId,
                    evidence,
                    cancellationToken: context.CancellationToken);

                await context.RespondAsync<IEvidenceExtractionResult>(new
                {
                    command.ExperimentId,
                    command.StepExecutionId,
                    Evidence = savedEvidence,
                    Success = true
                });

                _logger.LogInformation("Evidence Extraction completed. Extracted {Count} items.", savedEvidence.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing evidence extraction request");
                
                await context.RespondAsync<IEvidenceExtractionResult>(new
                {
                    command.ExperimentId,
                    command.StepExecutionId,
                    Evidence = new List<Evidence>(),
                    Success = false,
                    ErrorMessage = ex.Message
                });
            }
        }
    }
}
