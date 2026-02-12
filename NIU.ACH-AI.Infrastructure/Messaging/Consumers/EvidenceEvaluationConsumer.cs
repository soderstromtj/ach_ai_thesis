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
        private readonly IWorkflowPersistence _workflowPersistence;
        private readonly IWorkflowResultPersistence _workflowResultPersistence;
        private readonly ILogger<EvidenceEvaluationConsumer> _logger;

        public EvidenceEvaluationConsumer(
            IOrchestrationExecutor orchestrationExecutor,
            IOrchestrationFactoryProvider factoryProvider,
            IWorkflowPersistence workflowPersistence,
            IWorkflowResultPersistence workflowResultPersistence,
            ILogger<EvidenceEvaluationConsumer> logger)
        {
            _orchestrationExecutor = orchestrationExecutor;
            _factoryProvider = factoryProvider;
            _workflowPersistence = workflowPersistence;
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
                
                var createdStepContext = await _workflowPersistence.CreateStepExecutionAsync(
                    command.ExperimentId,
                    command.Configuration,
                    command.StepContext.StepExecutionId == Guid.Empty ? null : command.StepContext.StepExecutionId,
                    context.CancellationToken);

                // Use the persisted ID
                stepExecutionContext.StepExecutionId = createdStepContext.StepExecutionId;
                _logger.LogInformation("Step Execution Created with ID: {StepExecutionId}", stepExecutionContext.StepExecutionId);
                
                // Extract lists from Input (replaces previous Coordinator logic)
                var hypothesesToEvaluate = command.Input.HypothesisResult?.Hypotheses ?? new List<Hypothesis>();
                var evidenceList = command.Input.EvidenceResult?.Evidence ?? new List<Evidence>();
                
                _logger.LogInformation("Evaluating {EvidenceCount} evidence items against {HypothesisCount} hypotheses", 
                    evidenceList.Count, hypothesesToEvaluate.Count);

                var evaluations = new List<EvidenceHypothesisEvaluation>();
                var factory = _factoryProvider.CreateFactory<EvidenceHypothesisEvaluation>(command.Configuration);

                _logger.LogInformation("Starting evaluation loop for {EvidenceCount} evidence items x {HypothesisCount} hypotheses", evidenceList.Count, hypothesesToEvaluate.Count);

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

                        var evaluationResult = await _orchestrationExecutor.ExecuteAsync(
                            factory,
                            evaluationInput,
                            stepExecutionContext,
                            context.CancellationToken);

                        _logger.LogInformation("Executed evaluation. Score: {Score}", evaluationResult.Score);

                        // Reassign Hypothesis and Evidence because LLM response can't guarantee they remain unchanged
                        evaluationResult.Hypothesis = hypothesis;
                        evaluationResult.Evidence = evidence;

                        // Persist incrementally (optional but good for safety)
                         await _workflowResultPersistence.SaveEvaluationAsync(
                            stepExecutionContext.StepExecutionId,
                            evaluationResult,
                            command.HypothesisStepExecutionId,
                            command.EvidenceStepExecutionId,
                            context.CancellationToken);

                        evaluations.AddRange(evaluationResult);
                    }
                }
                _logger.LogInformation("Finished evaluation loop. Total evaluations generated: {Count}", evaluations.Count);

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
                    Evaluations = evaluations,
                    Success = true
                };

                await context.Publish<IEvidenceEvaluationResult>(resultMessage);
                // REMOVED: await context.RespondAsync<IEvidenceEvaluationResult>(resultMessage);

                _logger.LogInformation("Evaluation completed. Generated {Count} evaluations.", evaluations.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing evaluation request");
                
                // REMOVED: await context.RespondAsync<IEvidenceEvaluationResult>(...);
            }
        }
    }
}
