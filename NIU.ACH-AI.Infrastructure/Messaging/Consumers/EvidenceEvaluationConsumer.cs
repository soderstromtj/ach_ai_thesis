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
        private readonly IWorkflowPersistence _workflowPersistence;
        private readonly ILogger<EvidenceEvaluationConsumer> _logger;

        public EvidenceEvaluationConsumer(
            IWorkflowPersistence workflowPersistence,
            ILogger<EvidenceEvaluationConsumer> logger)
        {
            _workflowPersistence = workflowPersistence;
            _logger = logger;
        }

        public async Task Consume(ConsumeContext<IEvidenceEvaluationRequested> context)
        {
            var command = context.Message;
            _logger.LogInformation("Processing Evidence Evaluation Request (Dispatcher) for Experiment {ExperimentId}", command.ExperimentId);

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
                
                // Extract and deduplicate lists from Input
                // We use distinct by ID to ensure we don't process the same item multiple times
                var allHypotheses = command.Input.HypothesisResult?.Hypotheses ?? new List<Hypothesis>();
                var hypothesesToEvaluate = allHypotheses
                    .Where(h => h.HypothesisId != Guid.Empty) // Ensure valid ID
                    .GroupBy(h => h.HypothesisId) // Deduplicate by ID
                    .Select(g => g.First())
                    .ToList();

                var allEvidence = command.Input.EvidenceResult?.Evidence ?? new List<Evidence>();
                var evidenceList = allEvidence
                    .Where(e => e.EvidenceId != Guid.Empty) // Ensure valid ID
                    .GroupBy(e => e.EvidenceId) // Deduplicate by ID
                    .Select(g => g.First())
                    .ToList();
                
                int totalEvaluations = evidenceList.Count * hypothesesToEvaluate.Count;

                _logger.LogInformation("Dispatching {Total} evaluations ({EvidenceCount} evidence x {HypothesisCount} hypotheses) for Experiment {ExperimentId}", 
                    totalEvaluations, evidenceList.Count, hypothesesToEvaluate.Count, command.ExperimentId);

                // 1. Notify Saga of the batch start
                await context.Publish<IEvaluationBatchStarted>(new 
                {
                    command.ExperimentId,
                    createdStepContext.StepExecutionId,
                    TotalEvaluations = totalEvaluations
                });

                // 2. Dispatch individual evaluation commands
                var tasks = new List<Task>();
                foreach (var evidence in evidenceList)
                {
                    foreach (var hypothesis in hypothesesToEvaluate)
                    {
                        var evaluationInput = new OrchestrationPromptInput
                        {
                            KeyQuestion = command.Input.KeyQuestion,
                            Context = command.Input.Context,
                            TaskInstructions = command.Configuration.TaskInstructions,
                            EvidenceResult = new EvidenceResult { Evidence = new List<Evidence> { evidence } },
                            HypothesisResult = new HypothesisResult { Hypotheses = new List<Hypothesis> { hypothesis } }
                        };

                        var evaluationCommand = new 
                        {
                            command.ExperimentId,
                            createdStepContext.StepExecutionId,
                            command.HypothesisStepExecutionId,
                            command.EvidenceStepExecutionId,
                            command.Configuration,
                            Input = evaluationInput,
                            StepContext = createdStepContext
                        };

                        tasks.Add(context.Send<IEvaluateHypothesisEvidencePair>(evaluationCommand));
                    }
                }

                await Task.WhenAll(tasks);
                _logger.LogInformation("Dispatched {Count} evaluation commands.", tasks.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing evaluation request");
                throw; // Retry dispatcher if initial setup fails
            }
        }
    }
}
