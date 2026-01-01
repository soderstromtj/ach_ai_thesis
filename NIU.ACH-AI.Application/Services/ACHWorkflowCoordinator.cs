using MassTransit;
using Microsoft.Extensions.Logging;
using NIU.ACH_AI.Application.Configuration;
using NIU.ACH_AI.Application.DTOs;
using NIU.ACH_AI.Application.Interfaces;
using NIU.ACH_AI.Application.Messaging.Commands;
using NIU.ACH_AI.Application.Messaging.Events;
using NIU.ACH_AI.Domain.Entities;

namespace NIU.ACH_AI.Application.Services
{
    /// <summary>
    /// Coordinates the execution of ACH workflow steps.
    /// Manages state between steps and orchestrates the entire ACH process.
    /// </summary>
    public class ACHWorkflowCoordinator : IACHWorkflowCoordinator
    {
        private readonly IOrchestrationExecutor _orchestrationExecutor;
        private readonly IOrchestrationFactoryProvider _factoryProvider;
        private readonly IWorkflowPersistence _workflowPersistence;
        private readonly IAgentConfigurationPersistence _agentConfigurationPersistence;
        private readonly IWorkflowResultPersistence _workflowResultPersistence;
        private readonly IRequestClient<IBrainstormingRequested> _brainstormingClient;
        private readonly IRequestClient<IHypothesisRefinementRequested> _refinementClient;
        private readonly IRequestClient<IEvidenceExtractionRequested> _extractionClient;
        private readonly IRequestClient<IEvidenceEvaluationRequested> _evaluationClient;
        private readonly ILogger<ACHWorkflowCoordinator> _logger;

        public ACHWorkflowCoordinator(
            IOrchestrationExecutor orchestrationExecutor,
            IOrchestrationFactoryProvider factoryProvider,
            IWorkflowPersistence workflowPersistence,
            IAgentConfigurationPersistence agentConfigurationPersistence,
            IWorkflowResultPersistence workflowResultPersistence,
            IRequestClient<IBrainstormingRequested> brainstormingClient,
            IRequestClient<IHypothesisRefinementRequested> refinementClient,
            IRequestClient<IEvidenceExtractionRequested> extractionClient,
            IRequestClient<IEvidenceEvaluationRequested> evaluationClient,
            ILoggerFactory loggerFactory)
        {
            // Check if dependencies are null
            ArgumentNullException.ThrowIfNull(orchestrationExecutor);
            ArgumentNullException.ThrowIfNull(factoryProvider);
            ArgumentNullException.ThrowIfNull(workflowPersistence);
            ArgumentNullException.ThrowIfNull(agentConfigurationPersistence);
            ArgumentNullException.ThrowIfNull(workflowResultPersistence);
            ArgumentNullException.ThrowIfNull(brainstormingClient);
            ArgumentNullException.ThrowIfNull(loggerFactory);
            ArgumentNullException.ThrowIfNull(refinementClient);
            ArgumentNullException.ThrowIfNull(extractionClient);
            ArgumentNullException.ThrowIfNull(evaluationClient);

            _orchestrationExecutor = orchestrationExecutor;
            _factoryProvider = factoryProvider;
            _workflowPersistence = workflowPersistence;
            _agentConfigurationPersistence = agentConfigurationPersistence;
            _workflowResultPersistence = workflowResultPersistence;
            _brainstormingClient = brainstormingClient;
            _refinementClient = refinementClient;
            _extractionClient = extractionClient;
            _evaluationClient = evaluationClient;
            _logger = loggerFactory.CreateLogger<ACHWorkflowCoordinator>();
        }

        /// <summary>
        /// Executes the complete ACH workflow for the given experiment configuration.
        /// </summary>
        public async Task<ACHWorkflowResult> ExecuteWorkflowAsync(
            ExperimentConfiguration experimentConfig,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation($"Starting ACH workflow execution for experiment: {experimentConfig.Name}");

            var result = new ACHWorkflowResult
            {
                ExperimentId = experimentConfig.Id,
                ExperimentName = experimentConfig.Name
            };

            try
            {
                var scenarioId = await _workflowPersistence.CreateScenarioAsync(experimentConfig.Context, cancellationToken);
                var experimentId = await _workflowPersistence.CreateExperimentAsync(experimentConfig, scenarioId, cancellationToken);

                // Build the base input from experiment configuration
                var input = new OrchestrationPromptInput
                {
                    KeyQuestion = experimentConfig.KeyQuestion,
                    Context = experimentConfig.Context
                };

                Guid? hypothesisStepExecutionId = null;
                Guid? refinedHypothesisStepExecutionId = null;
                Guid? evidenceStepExecutionId = null;

                // Execute each ACH step in sequence
                foreach (var step in experimentConfig.ACHSteps)
                {
                    _logger.LogInformation($"Executing ACH step: {step.Name} (ID: {step.Id})");
                    Console.WriteLine($"\n{new string('=', 70)}");
                    Console.WriteLine($"Executing Step {step.Id}: {step.Name}");
                    Console.WriteLine(new string('=', 70));

                    // Create step execution context which tracks state for this step
                    var stepExecutionContext = await _workflowPersistence.CreateStepExecutionAsync(
                        experimentId,
                        step,
                        cancellationToken);

                    // Persist agent configurations for this step
                    var agentConfigurationIds = await _agentConfigurationPersistence.CreateAgentConfigurationsAsync(
                        stepExecutionContext.StepExecutionId,
                        step.AgentConfigurations,
                        cancellationToken);

                    // Link agent configuration IDs to step execution context
                    stepExecutionContext.AgentConfigurationIds = agentConfigurationIds;
                    var stepStart = DateTime.UtcNow;
                    await _workflowPersistence.UpdateStepExecutionStatusAsync(
                        stepExecutionContext.StepExecutionId,
                        "Running",
                        start: stepStart,
                        cancellationToken: cancellationToken);

                    // Update task instructions for current step
                    input.TaskInstructions = step.TaskInstructions;

                    // Execute the appropriate step based on configuration
                    try
                    {
                        // Execute the step and update workflow state
                        await ExecuteStepAsync(
                            step,
                            input,
                            result,
                            stepExecutionContext,
                            refinedHypothesisStepExecutionId ?? hypothesisStepExecutionId,
                            evidenceStepExecutionId,
                            cancellationToken);

                        var stepName = step.Name.ToLowerInvariant();
                        switch (stepName)
                        {
                            case "hypothesis brainstorming" or "hypothesisbrainstorming":
                                hypothesisStepExecutionId = stepExecutionContext.StepExecutionId;
                                break;

                            case "hypothesis evaluation" or "hypothesisevaluation" or "hypothesis refinement" or "hypothesisrefinement":
                                refinedHypothesisStepExecutionId = stepExecutionContext.StepExecutionId;
                                break;

                            case "evidence extraction" or "evidenceextraction":
                                evidenceStepExecutionId = stepExecutionContext.StepExecutionId;
                                break;
                        }
                        await _workflowPersistence.UpdateStepExecutionStatusAsync(
                            stepExecutionContext.StepExecutionId,
                            "Completed",
                            end: DateTime.UtcNow,
                            cancellationToken: cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        await _workflowPersistence.UpdateStepExecutionStatusAsync(
                            stepExecutionContext.StepExecutionId,
                            "Failed",
                            end: DateTime.UtcNow,
                            errorType: ex.GetType().Name,
                            errorMessage: ex.Message,
                            cancellationToken: cancellationToken);
                        throw;
                    }
                }

                result.Success = true;
                _logger.LogInformation($"Successfully completed ACH workflow for experiment: {experimentConfig.Name}");
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = ex.Message;
                _logger.LogError(ex, $"Error executing ACH workflow for experiment: {experimentConfig.Name}");
                throw;
            }

            return result;
        }

        /// <summary>
        /// Executes a single ACH step and updates the workflow state.
        /// </summary>
        /// <param name="stepConfig">The configuration for the step to execute.</param>
        /// <param name="input">The orchestration input prompt and context.</param>
        /// <param name="workflowResult">The detailed workflow result object to update.</param>
        /// <param name="stepExecutionContext">The persistence context/ID for this step execution.</param>
        /// <param name="hypothesisStepExecutionId">The ID of the hypothesis generation step (if needed for linking).</param>
        /// <param name="evidenceStepExecutionId">The ID of the evidence extraction step (if needed for linking).</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        private async Task ExecuteStepAsync(
            ACHStepConfiguration stepConfig,
            OrchestrationPromptInput input,
            ACHWorkflowResult workflowResult,
            StepExecutionContext stepExecutionContext,
            Guid? hypothesisStepExecutionId,
            Guid? evidenceStepExecutionId,
            CancellationToken cancellationToken)
        {
            // Determine which step to execute based on the step name
            var stepName = stepConfig.Name.ToLowerInvariant();

            switch (stepName)
            {
                case "hypothesis brainstorming" or "hypothesisbrainstorming":
                    await ExecuteHypothesisBrainstormingAsync(stepConfig, input, workflowResult, stepExecutionContext, cancellationToken);
                    break;

                case "hypothesis evaluation" or "hypothesisevaluation" or "hypothesis refinement" or "hypothesisrefinement":
                    await ExecuteHypothesisRefinementAsync(stepConfig, input, workflowResult, stepExecutionContext, cancellationToken);
                    break;

                case "evidence extraction" or "evidenceextraction":
                    await ExecuteEvidenceExtractionAsync(stepConfig, input, workflowResult, stepExecutionContext, cancellationToken);
                    break;

                case "evidence hypothesis evaluation" or "evidencehypothesisevaluation" or "evidence evaluation" or "evidenceevaluation":
                    await ExecuteEvidenceHypothesisEvaluationAsync(
                        stepConfig,
                        input,
                        workflowResult,
                        stepExecutionContext,
                        hypothesisStepExecutionId,
                        evidenceStepExecutionId,
                        cancellationToken);
                    break;

                default:
                    throw new InvalidOperationException(
                        $"Unknown ACH step: {stepConfig.Name}. Unable to execute this step type.");
            }
        }

        /// <summary>
        /// Executes the hypothesis brainstorming step using the configured factory.
        /// </summary>
        /// <param name="stepConfig">The step configuration.</param>
        /// <param name="input">The orchestration input.</param>
        /// <param name="workflowResult">The workflow result to update with generated hypotheses.</param>
        /// <param name="stepExecutionContext">The step execution context.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <summary>
        /// Executes the hypothesis brainstorming step using the configured factory.
        /// </summary>
        /// <param name="stepConfig">The step configuration.</param>
        /// <param name="input">The orchestration input.</param>
        /// <param name="workflowResult">The workflow result to update with generated hypotheses.</param>
        /// <param name="stepExecutionContext">The step execution context.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        private async Task ExecuteHypothesisBrainstormingAsync(
            ACHStepConfiguration stepConfig,
            OrchestrationPromptInput input,
            ACHWorkflowResult workflowResult,
            StepExecutionContext stepExecutionContext,
            CancellationToken cancellationToken)
        {
            // Phase 2: Use MassTransit Request/Response
            _logger.LogInformation("Sending Brainstorming Request to Message Bus...");

            var response = await _brainstormingClient.GetResponse<IBrainstormingResult>(new
                {
                    stepExecutionContext.ExperimentId,
                    stepExecutionContext.StepExecutionId,
                    Input = input,
                    Configuration = stepConfig,
                    StepContext = stepExecutionContext,
                    Timestamp = DateTime.UtcNow
                }, cancellationToken, timeout: RequestTimeout.After(m: 5));
            
            if (!response.Message.Success)
            {
                throw new Exception($"Brainstorming failed: {response.Message.ErrorMessage}");
            }

            var hypotheses = response.Message.Hypotheses;

            // Update workflow result and input for next step
            workflowResult.Hypotheses = hypotheses;
            input.HypothesisResult = new HypothesisResult { Hypotheses = hypotheses };

            _logger.LogInformation($"Received {hypotheses.Count} hypotheses from Message Bus worker.");
        }

        /// <summary>
        /// Executes the hypothesis refinement/evaluation step using the configured factory.
        /// </summary>
        /// <param name="stepConfig">The step configuration.</param>
        /// <param name="input">The orchestration input (including initial hypotheses).</param>
        /// <param name="workflowResult">The workflow result to update with refined hypotheses.</param>
        /// <param name="stepExecutionContext">The step execution context.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        private async Task ExecuteHypothesisRefinementAsync(
            ACHStepConfiguration stepConfig,
            OrchestrationPromptInput input,
            ACHWorkflowResult workflowResult,
            StepExecutionContext stepExecutionContext,
            CancellationToken cancellationToken)
        {
            _logger.LogInformation("Sending Refinement Request to Message Bus...");

            var response = await _refinementClient.GetResponse<IHypothesisRefinementResult>(new
            {
                stepExecutionContext.ExperimentId,
                stepExecutionContext.StepExecutionId,
                Input = input,
                Configuration = stepConfig,
                StepContext = stepExecutionContext,
                Timestamp = DateTime.UtcNow
            }, cancellationToken, timeout: RequestTimeout.After(m: 5));

            if (!response.Message.Success)
            {
                throw new Exception($"Refinement failed: {response.Message.ErrorMessage}");
            }

            var refinedHypotheses = response.Message.RefinedHypotheses;

            // Update workflow result and input for next step with saved entities (containing IDs)
            workflowResult.RefinedHypotheses = refinedHypotheses;
            input.HypothesisResult = new HypothesisResult { Hypotheses = refinedHypotheses };

            _logger.LogInformation($"Refined to {refinedHypotheses.Count} hypotheses from Message Bus worker.");
        }

        /// <summary>
        /// Executes the evidence extraction step using the configured factory.
        /// </summary>
        /// <param name="stepConfig">The step configuration.</param>
        /// <param name="input">The orchestration input.</param>
        /// <param name="workflowResult">The workflow result to update with extracted evidence.</param>
        /// <param name="stepExecutionContext">The step execution context.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        private async Task ExecuteEvidenceExtractionAsync(
            ACHStepConfiguration stepConfig,
            OrchestrationPromptInput input,
            ACHWorkflowResult workflowResult,
            StepExecutionContext stepExecutionContext,
            CancellationToken cancellationToken)
        {
            _logger.LogInformation("Sending Evidence Extraction Request to Message Bus...");

            var response = await _extractionClient.GetResponse<IEvidenceExtractionResult>(new
            {
                stepExecutionContext.ExperimentId,
                stepExecutionContext.StepExecutionId,
                Input = input,
                Configuration = stepConfig,
                StepContext = stepExecutionContext,
                Timestamp = DateTime.UtcNow
            }, cancellationToken, timeout: RequestTimeout.After(m: 5));

            if (!response.Message.Success)
            {
                throw new Exception($"Evidence Extraction failed: {response.Message.ErrorMessage}");
            }

            var evidence = response.Message.Evidence;

            // Update workflow result and input for next step with saved entities (containing IDs)
            workflowResult.Evidence = evidence;
            input.EvidenceResult = new EvidenceResult { Evidence = evidence };

            _logger.LogInformation($"Extracted {evidence.Count} pieces of evidence from Message Bus worker.");
        }

        /// <summary>
        /// Executes the evidence-hypothesis evaluation step.
        /// Evaluates each piece of evidence against each hypothesis utilizing the configured factory.
        /// </summary>
        /// <param name="stepConfig">The step configuration.</param>
        /// <param name="input">The orchestration input.</param>
        /// <param name="workflowResult">The workflow result to update with evaluation results.</param>
        /// <param name="stepExecutionContext">The step execution context.</param>
        /// <param name="hypothesisStepExecutionId">The source hypothesis step ID.</param>
        /// <param name="evidenceStepExecutionId">The source evidence step ID.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <exception cref="InvalidOperationException">Thrown if hypothesis or evidence step IDs are missing.</exception>
        private async Task ExecuteEvidenceHypothesisEvaluationAsync(
            ACHStepConfiguration stepConfig,
            OrchestrationPromptInput input,
            ACHWorkflowResult workflowResult,
            StepExecutionContext stepExecutionContext,
            Guid? hypothesisStepExecutionId,
            Guid? evidenceStepExecutionId,
            CancellationToken cancellationToken)
        {
            // Prepare Input with correct lists for the consumer to iterate
            // Input already has KeyQuestion and Context. We need explicitly populate HypothesisResult/EvidenceResult if not present
            // However, the previous steps (Extraction/Refinement) updated 'input.HypothesisResult' and 'input.EvidenceResult'
            // So 'input' should be ready to go.

            _logger.LogInformation("Sending Evidence Evaluation Request to Message Bus...");

            var response = await _evaluationClient.GetResponse<IEvidenceEvaluationResult>(new
            {
                stepExecutionContext.ExperimentId,
                stepExecutionContext.StepExecutionId,
                Input = input,
                Configuration = stepConfig,
                StepContext = stepExecutionContext,
                HypothesisStepExecutionId = hypothesisStepExecutionId.Value,
                EvidenceStepExecutionId = evidenceStepExecutionId.Value,
                Timestamp = DateTime.UtcNow
            }, cancellationToken, timeout: RequestTimeout.After(m: 10)); // Evaluation is long running!

             if (!response.Message.Success)
            {
                throw new Exception($"Evidence Evaluation failed: {response.Message.ErrorMessage}");
            }

            workflowResult.Evaluations = response.Message.Evaluations;
            _logger.LogInformation($"Completed {response.Message.Evaluations.Count} evidence-hypothesis evaluations from Message Bus worker.");
        }
    }
}
