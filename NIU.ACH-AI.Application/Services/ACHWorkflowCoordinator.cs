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
        private readonly ILogger<ACHWorkflowCoordinator> _logger;

        public ACHWorkflowCoordinator(
            IOrchestrationExecutor orchestrationExecutor,
            IOrchestrationFactoryProvider factoryProvider,
            ILoggerFactory loggerFactory)
        {
            _orchestrationExecutor = orchestrationExecutor;
            _factoryProvider = factoryProvider;
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
                // Build the base input from experiment configuration
                var input = new OrchestrationPromptInput
                {
                    KeyQuestion = experimentConfig.KeyQuestion,
                    Context = experimentConfig.Context
                };

                // Execute each ACH step in sequence
                foreach (var step in experimentConfig.ACHSteps)
                {
                    _logger.LogInformation($"Executing ACH step: {step.Name} (ID: {step.Id})");
                    Console.WriteLine($"\n{new string('=', 70)}");
                    Console.WriteLine($"Executing Step {step.Id}: {step.Name}");
                    Console.WriteLine(new string('=', 70));

                    // Update task instructions for current step
                    input.TaskInstructions = step.TaskInstructions;

                    // Execute the appropriate step based on configuration
                    await ExecuteStepAsync(step, input, result, cancellationToken);
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
            CancellationToken cancellationToken)
        {
            var stepName = stepConfig.Name.ToLowerInvariant();

            switch (stepName)
            {
                case "hypothesis brainstorming" or "hypothesisbrainstorming":
                    await ExecuteHypothesisBrainstormingAsync(stepConfig, input, workflowResult, cancellationToken);
                    break;

                case "hypothesis evaluation" or "hypothesisevaluation" or "hypothesis refinement" or "hypothesisrefinement":
                    await ExecuteHypothesisRefinementAsync(stepConfig, input, workflowResult, cancellationToken);
                    break;

                case "evidence extraction" or "evidenceextraction":
                    await ExecuteEvidenceExtractionAsync(stepConfig, input, workflowResult, cancellationToken);
                    break;

                case "evidence hypothesis evaluation" or "evidencehypothesisevaluation" or "evidence evaluation" or "evidenceevaluation":
                    await ExecuteEvidenceHypothesisEvaluationAsync(stepConfig, input, workflowResult, cancellationToken);
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
            CancellationToken cancellationToken)
        {
            var factory = _factoryProvider.CreateFactory<List<Hypothesis>>(stepConfig);
            var hypotheses = await _orchestrationExecutor.ExecuteAsync(factory, input, cancellationToken);

            // Update workflow result and input for next step
            workflowResult.Hypotheses = hypotheses;
            input.HypothesisResult = new HypothesisResult { Hypotheses = hypotheses };

            _logger.LogInformation($"Generated {hypotheses.Count} hypotheses");
        }

        /// <summary>
        /// Executes the hypothesis refinement/evaluation step.
        /// </summary>
        private async Task ExecuteHypothesisRefinementAsync(
            ACHStepConfiguration stepConfig,
            OrchestrationPromptInput input,
            ACHWorkflowResult workflowResult,
            CancellationToken cancellationToken)
        {
            var factory = _factoryProvider.CreateFactory<List<Hypothesis>>(stepConfig);
            var refinedHypotheses = await _orchestrationExecutor.ExecuteAsync(factory, input, cancellationToken);

            // Update workflow result and input for next step
            workflowResult.RefinedHypotheses = refinedHypotheses;
            input.HypothesisResult = new HypothesisResult { Hypotheses = refinedHypotheses };

            _logger.LogInformation($"Refined to {refinedHypotheses.Count} hypotheses");
        }

        /// <summary>
        /// Executes the evidence extraction step.
        /// </summary>
        private async Task ExecuteEvidenceExtractionAsync(
            ACHStepConfiguration stepConfig,
            OrchestrationPromptInput input,
            ACHWorkflowResult workflowResult,
            CancellationToken cancellationToken)
        {
            var factory = _factoryProvider.CreateFactory<List<Evidence>>(stepConfig);
            var evidence = await _orchestrationExecutor.ExecuteAsync(factory, input, cancellationToken);

            // Update workflow result and input for next step
            workflowResult.Evidence = evidence;
            input.EvidenceResult = new EvidenceResult { Evidence = evidence };

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
            CancellationToken cancellationToken)
        {
            var evaluations = new List<EvidenceHypothesisEvaluation>();

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
                    var evaluationResults = await _orchestrationExecutor.ExecuteAsync(factory, evaluationInput, cancellationToken);

                    evaluations.AddRange(evaluationResults);
                }
            }

            workflowResult.Evaluations = evaluations;
            _logger.LogInformation($"Completed {evaluations.Count} evidence-hypothesis evaluations");
        }
    }
}
