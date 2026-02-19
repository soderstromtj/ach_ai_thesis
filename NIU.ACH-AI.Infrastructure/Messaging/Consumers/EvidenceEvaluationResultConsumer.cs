using MassTransit;
using Microsoft.Extensions.Logging;
using NIU.ACH_AI.Application.Messaging.Events;
using NIU.ACH_AI.Application.Interfaces;

namespace NIU.ACH_AI.Infrastructure.Messaging.Consumers
{
    public class EvidenceEvaluationResultConsumer : IConsumer<IEvidenceEvaluationResult>
    {
        private readonly IWorkflowPersistence _workflowPersistence;
        private readonly ILogger<EvidenceEvaluationResultConsumer> _logger;

        public EvidenceEvaluationResultConsumer(
            IWorkflowPersistence workflowPersistence, 
            ILogger<EvidenceEvaluationResultConsumer> logger)
        {
            _workflowPersistence = workflowPersistence;
            _logger = logger;
        }

        public async Task Consume(ConsumeContext<IEvidenceEvaluationResult> context)
        {
            _logger.LogInformation("Handling Evidence Evaluation Result for Experiment {ExperimentId}", context.Message.ExperimentId);

            if (context.Message.Success)
            {
                await _workflowPersistence.UpdateStepExecutionStatusAsync(
                    context.Message.StepExecutionId,
                    "Completed",
                    end: DateTime.UtcNow,
                    cancellationToken: context.CancellationToken);

                _logger.LogInformation("Marked StepExecution {StepExecutionId} as Completed.", context.Message.StepExecutionId);
            }
            else
            {
                 await _workflowPersistence.UpdateStepExecutionStatusAsync(
                    context.Message.StepExecutionId,
                    "Failed",
                    end: DateTime.UtcNow,
                    errorMessage: context.Message.ErrorMessage,
                    cancellationToken: context.CancellationToken);
                 
                 _logger.LogWarning("Marked StepExecution {StepExecutionId} as Failed.", context.Message.StepExecutionId);
            }
        }
    }
}
