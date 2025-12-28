using Microsoft.Extensions.Logging;
using NIU.ACH_AI.Application.Configuration;
using NIU.ACH_AI.Application.DTOs;
using NIU.ACH_AI.Application.Interfaces;
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
        private readonly ILogger<ACHWorkflowCoordinator> _logger;

        public ACHWorkflowCoordinator(
            IOrchestrationExecutor orchestrationExecutor,
            IOrchestrationFactoryProvider factoryProvider,
            IWorkflowPersistence workflowPersistence,
            IAgentConfigurationPersistence agentConfigurationPersistence,
            IWorkflowResultPersistence workflowResultPersistence,
            ILoggerFactory loggerFactory)
        {
            // Check if dependencies are null
            ArgumentNullException.ThrowIfNull(orchestrationExecutor);
            ArgumentNullException.ThrowIfNull(factoryProvider);
            ArgumentNullException.ThrowIfNull(workflowPersistence);
            ArgumentNullException.ThrowIfNull(agentConfigurationPersistence);
            ArgumentNullException.ThrowIfNull(workflowResultPersistence);
            ArgumentNullException.ThrowIfNull(loggerFactory);

            _orchestrationExecutor = orchestrationExecutor;
            _factoryProvider = factoryProvider;
            _workflowPersistence = workflowPersistence;
            _agentConfigurationPersistence = agentConfigurationPersistence;
            _workflowResultPersistence = workflowResultPersistence;
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

                    var stepExecutionContext = await _workflowPersistence.CreateStepExecutionAsync(
                        experimentId,
                        step,
                        cancellationToken);

                    var agentConfigurationIds = await _agentConfigurationPersistence.CreateAgentConfigurationsAsync(
                        stepExecutionContext.StepExecutionId,
                        step.AgentConfigurations,
                        cancellationToken);

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
        private async Task ExecuteStepAsync(
            ACHStepConfiguration stepConfig,
            OrchestrationPromptInput input,
            ACHWorkflowResult workflowResult,
            StepExecutionContext stepExecutionContext,
            Guid? hypothesisStepExecutionId,
            Guid? evidenceStepExecutionId,
            CancellationToken cancellationToken)
        {
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
        /// Executes the hypothesis brainstorming step.
        /// </summary>
        private async Task ExecuteHypothesisBrainstormingAsync(
            ACHStepConfiguration stepConfig,
            OrchestrationPromptInput input,
            ACHWorkflowResult workflowResult,
            StepExecutionContext stepExecutionContext,
            CancellationToken cancellationToken)
        {
            var factory = _factoryProvider.CreateFactory<List<Hypothesis>>(stepConfig);
            var hypotheses = await _orchestrationExecutor.ExecuteAsync(
                factory,
                input,
                stepExecutionContext,
                cancellationToken);

            // Update workflow result and input for next step
            workflowResult.Hypotheses = hypotheses;
            input.HypothesisResult = new HypothesisResult { Hypotheses = hypotheses };

            await _workflowResultPersistence.SaveHypothesesAsync(
                stepExecutionContext.StepExecutionId,
                hypotheses,
                isRefined: false,
                cancellationToken: cancellationToken);

            _logger.LogInformation($"Generated {hypotheses.Count} hypotheses");
        }

        /// <summary>
        /// Executes the hypothesis refinement/evaluation step.
        /// </summary>
        private async Task ExecuteHypothesisRefinementAsync(
            ACHStepConfiguration stepConfig,
            OrchestrationPromptInput input,
            ACHWorkflowResult workflowResult,
            StepExecutionContext stepExecutionContext,
            CancellationToken cancellationToken)
        {
            var factory = _factoryProvider.CreateFactory<List<Hypothesis>>(stepConfig);
            var refinedHypotheses = await _orchestrationExecutor.ExecuteAsync(
                factory,
                input,
                stepExecutionContext,
                cancellationToken);

            // Update workflow result and input for next step
            workflowResult.RefinedHypotheses = refinedHypotheses;
            input.HypothesisResult = new HypothesisResult { Hypotheses = refinedHypotheses };

            await _workflowResultPersistence.SaveHypothesesAsync(
                stepExecutionContext.StepExecutionId,
                refinedHypotheses,
                isRefined: true,
                cancellationToken: cancellationToken);

            _logger.LogInformation($"Refined to {refinedHypotheses.Count} hypotheses");
        }

        /// <summary>
        /// Executes the evidence extraction step.
        /// </summary>
        private async Task ExecuteEvidenceExtractionAsync(
            ACHStepConfiguration stepConfig,
            OrchestrationPromptInput input,
            ACHWorkflowResult workflowResult,
            StepExecutionContext stepExecutionContext,
            CancellationToken cancellationToken)
        {
            var factory = _factoryProvider.CreateFactory<List<Evidence>>(stepConfig);
            var evidence = await _orchestrationExecutor.ExecuteAsync(
                factory,
                input,
                stepExecutionContext,
                cancellationToken);

            // Update workflow result and input for next step
            workflowResult.Evidence = evidence;
            input.EvidenceResult = new EvidenceResult { Evidence = evidence };

            await _workflowResultPersistence.SaveEvidenceAsync(
                stepExecutionContext.StepExecutionId,
                evidence,
                cancellationToken: cancellationToken);

            _logger.LogInformation($"Extracted {evidence.Count} pieces of evidence");
        }

        /// <summary>
        /// Executes the evidence-hypothesis evaluation step.
        /// Evaluates each piece of evidence against each hypothesis.
        /// </summary>
        private async Task ExecuteEvidenceHypothesisEvaluationAsync(
            ACHStepConfiguration stepConfig,
            OrchestrationPromptInput input,
            ACHWorkflowResult workflowResult,
            StepExecutionContext stepExecutionContext,
            Guid? hypothesisStepExecutionId,
            Guid? evidenceStepExecutionId,
            CancellationToken cancellationToken)
        {
            var evaluations = new List<EvidenceHypothesisEvaluation>();

            if (hypothesisStepExecutionId == null || hypothesisStepExecutionId == Guid.Empty)
            {
                throw new InvalidOperationException(
                    "Hypothesis step execution ID is required before evidence-hypothesis evaluation.");
            }

            if (evidenceStepExecutionId == null || evidenceStepExecutionId == Guid.Empty)
            {
                throw new InvalidOperationException(
                    "Evidence step execution ID is required before evidence-hypothesis evaluation.");
            }

            // Use refined hypotheses if available, otherwise use initial hypotheses
            var hypothesesToEvaluate = workflowResult.RefinedHypotheses ?? workflowResult.Hypotheses ?? new List<Hypothesis>();
            var evidenceList = workflowResult.Evidence ?? new List<Evidence>();

            _logger.LogInformation(
                $"Evaluating {evidenceList.Count} pieces of evidence against {hypothesesToEvaluate.Count} hypotheses");

            // Evaluate each evidence-hypothesis pair
            foreach (var evidence in evidenceList)
            {
                foreach (var hypothesis in hypothesesToEvaluate)
                {
                    Console.WriteLine(
                        $"\nEvaluating Evidence vs Hypothesis:\n" +
                        $"  Evidence: {evidence.Claim}\n" +
                        $"  Hypothesis: {hypothesis.HypothesisText}");

                    // Create input with single evidence and hypothesis
                    var evaluationInput = new OrchestrationPromptInput
                    {
                        KeyQuestion = input.KeyQuestion,
                        Context = input.Context,
                        TaskInstructions = stepConfig.TaskInstructions,
                        EvidenceResult = new EvidenceResult { Evidence = new List<Evidence> { evidence } },
                        HypothesisResult = new HypothesisResult { Hypotheses = new List<Hypothesis> { hypothesis } }
                    };

                    var factory = _factoryProvider.CreateFactory<List<EvidenceHypothesisEvaluation>>(stepConfig);
                    var evaluationResults = await _orchestrationExecutor.ExecuteAsync(
                        factory,
                        evaluationInput,
                        stepExecutionContext,
                        cancellationToken);

                    evaluations.AddRange(evaluationResults);
                }
            }

            workflowResult.Evaluations = evaluations;
            await _workflowResultPersistence.SaveEvaluationsAsync(
                stepExecutionContext.StepExecutionId,
                evaluations,
                hypothesisStepExecutionId.Value,
                evidenceStepExecutionId.Value,
                cancellationToken);
            _logger.LogInformation($"Completed {evaluations.Count} evidence-hypothesis evaluations");
        }
    }
}
