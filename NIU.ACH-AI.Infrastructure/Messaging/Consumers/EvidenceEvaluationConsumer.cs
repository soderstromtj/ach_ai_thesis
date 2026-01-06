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
    /// Consumer that handles Evidence-Hypothesis Evaluation requests.
    /// </summary>
    public class EvidenceEvaluationConsumer : IConsumer<IEvidenceEvaluationRequested>
    {
        private readonly IOrchestrationExecutor _orchestrationExecutor;
        private readonly IOrchestrationFactoryProvider _factoryProvider;
        private readonly IWorkflowResultPersistence _workflowResultPersistence;
        private readonly ILogger<EvidenceEvaluationConsumer> _logger;

        public EvidenceEvaluationConsumer(
            IOrchestrationExecutor orchestrationExecutor,
            IOrchestrationFactoryProvider factoryProvider,
            IWorkflowResultPersistence workflowResultPersistence,
            ILogger<EvidenceEvaluationConsumer> logger)
        {
            _orchestrationExecutor = orchestrationExecutor;
            _factoryProvider = factoryProvider;
            _workflowResultPersistence = workflowResultPersistence;
            _logger = logger;
        }

        public async Task Consume(ConsumeContext<IEvidenceEvaluationRequested> context)
        {
            var command = context.Message;
            _logger.LogInformation("Processing Evidence Evaluation Request for Experiment {ExperimentId}", command.ExperimentId);

            try
            {
                var stepExecutionContext = command.StepContext;
                
                // Extract lists from Input (replaces previous Coordinator logic)
                var hypothesesToEvaluate = command.Input.HypothesisResult?.Hypotheses ?? new List<Hypothesis>();
                var evidenceList = command.Input.EvidenceResult?.Evidence ?? new List<Evidence>();
                
                _logger.LogInformation("Evaluating {EvidenceCount} evidence items against {HypothesisCount} hypotheses", 
                    evidenceList.Count, hypothesesToEvaluate.Count);

                var evaluations = new List<EvidenceHypothesisEvaluation>();
                var factory = _factoryProvider.CreateFactory<List<EvidenceHypothesisEvaluation>>(command.Configuration);

                foreach (var evidence in evidenceList)
                {
                    foreach (var hypothesis in hypothesesToEvaluate)
                    {
                        // Create scoped input for this specific pair
                        var evaluationInput = new OrchestrationPromptInput
                        {
                            KeyQuestion = command.Input.KeyQuestion,
                            Context = command.Input.Context,
                            TaskInstructions = command.Configuration.TaskInstructions, // Use config from command
                            EvidenceResult = new EvidenceResult { Evidence = new List<Evidence> { evidence } },
                            HypothesisResult = new HypothesisResult { Hypotheses = new List<Hypothesis> { hypothesis } }
                        };

                        var evaluationResults = await _orchestrationExecutor.ExecuteAsync(
                            factory,
                            evaluationInput,
                            stepExecutionContext,
                            context.CancellationToken);

                        foreach (var result in evaluationResults)
                        {
                            result.Hypothesis = hypothesis;
                            result.Evidence = evidence;
                        }

                        // Persist incrementally (optional but good for safety)
                         await _workflowResultPersistence.SaveEvaluationsAsync(
                            stepExecutionContext.StepExecutionId,
                            evaluationResults,
                            command.HypothesisStepExecutionId,
                            command.EvidenceStepExecutionId,
                            context.CancellationToken);

                        evaluations.AddRange(evaluationResults);
                    }
                }

                var resultMessage = new
                {
                    command.ExperimentId,
                    command.StepExecutionId,
                    Evaluations = evaluations,
                    Success = true
                };

                await context.Publish<IEvidenceEvaluationResult>(resultMessage);
                await context.RespondAsync<IEvidenceEvaluationResult>(resultMessage);

                _logger.LogInformation("Evaluation completed. Generated {Count} evaluations.", evaluations.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing evaluation request");
                
                await context.RespondAsync<IEvidenceEvaluationResult>(new
                {
                    command.ExperimentId,
                    command.StepExecutionId,
                    Evaluations = new List<EvidenceHypothesisEvaluation>(),
                    Success = false,
                    ErrorMessage = ex.Message
                });
            }
        }
    }
}
