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
    /// Evaluates one specific piece of evidence against one specific hypothesis.
    /// Used as a concurrent worker in the overall evaluation phase.
    /// </summary>
    public class SingleEvidenceEvaluationConsumer : IConsumer<IEvaluateHypothesisEvidencePair>
    {
        private readonly IOrchestrationExecutor _orchestrationExecutor;
        private readonly IOrchestrationFactoryProvider _factoryProvider;
        private readonly IWorkflowResultPersistence _workflowResultPersistence;
        private readonly ILogger<SingleEvidenceEvaluationConsumer> _logger;

        public SingleEvidenceEvaluationConsumer(
            IOrchestrationExecutor orchestrationExecutor,
            IOrchestrationFactoryProvider factoryProvider,
            IWorkflowResultPersistence workflowResultPersistence,
            ILogger<SingleEvidenceEvaluationConsumer> logger)
        {
            _orchestrationExecutor = orchestrationExecutor;
            _factoryProvider = factoryProvider;
            _workflowResultPersistence = workflowResultPersistence;
            _logger = logger;
        }

        public async Task Consume(ConsumeContext<IEvaluateHypothesisEvidencePair> context)
        {
            var command = context.Message;
            var evidence = command.Input.EvidenceResult!.Evidence.First();
            var hypothesis = command.Input.HypothesisResult!.Hypotheses.First();

            _logger.LogInformation("Evaluating Pair: Evidence {EvidenceId} vs Hypothesis {HypothesisId} (Experiment {ExperimentId})",
                evidence.EvidenceId, hypothesis.HypothesisId, command.ExperimentId);

            try
            {
                var factory = _factoryProvider.CreateFactory<EvidenceHypothesisEvaluation>(command.Configuration);

                var evaluationResult = await _orchestrationExecutor.ExecuteAsync(
                    factory,
                    command.Input,
                    command.StepContext,
                    context.CancellationToken);

                // Ensure relation is preserved
                evaluationResult.Hypothesis = hypothesis;
                evaluationResult.Evidence = evidence;

                _logger.LogInformation("Evaluation Score: {Score}", evaluationResult.Score);

                // Persist Result
                await _workflowResultPersistence.SaveEvaluationAsync(
                    command.StepExecutionId,
                    evaluationResult,
                    command.HypothesisStepExecutionId,
                    command.EvidenceStepExecutionId,
                    context.CancellationToken);

                // Publish Completion Event
                await context.Publish<IPairEvaluated>(new 
                {
                    command.ExperimentId,
                    command.StepExecutionId,
                    Evaluation = evaluationResult,
                    Success = true
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error evaluating pair: Evidence {EvidenceId} vs Hypothesis {HypothesisId}", 
                    evidence.EvidenceId, hypothesis.HypothesisId);
                
                // Publish Failure Event 
                await context.Publish<IPairEvaluated>(new 
                {
                    command.ExperimentId,
                    command.StepExecutionId,
                    Evaluation = (EvidenceHypothesisEvaluation?)null,
                    Success = false,
                    ErrorMessage = ex.Message
                });
                
                // We rethrow to let MassTransit retry this specific single evaluation if it was a transient error
                throw; 
            }
        }
    }
}
