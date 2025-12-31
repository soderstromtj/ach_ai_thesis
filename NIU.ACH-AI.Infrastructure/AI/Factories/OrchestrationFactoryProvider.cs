using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NIU.ACH_AI.Application.Configuration;
using NIU.ACH_AI.Application.Interfaces;
using NIU.ACH_AI.Domain.Entities;

namespace NIU.ACH_AI.Infrastructure.AI.Factories
{
    /// <summary>
    /// Factory provider that creates the appropriate orchestration factory based on ACH step configuration.
    /// Uses a mapping strategy to determine which factory to instantiate.
    /// </summary>
    public class OrchestrationFactoryProvider : IOrchestrationFactoryProvider
    {
        private readonly IOrchestrationExecutor _orchestrationExecutor;
        private readonly IAgentResponsePersistence _agentResponsePersistence;
        private readonly ITokenUsageExtractor _tokenUsageExtractor;

        public OrchestrationFactoryProvider(
            IOrchestrationExecutor orchestrationExecutor,
            IAgentResponsePersistence agentResponsePersistence,
            ITokenUsageExtractor tokenUsageExtractor)
        {
            _orchestrationExecutor = orchestrationExecutor;
            _agentResponsePersistence = agentResponsePersistence ?? throw new ArgumentNullException(nameof(agentResponsePersistence));
            _tokenUsageExtractor = tokenUsageExtractor ?? throw new ArgumentNullException(nameof(tokenUsageExtractor));
        }

        /// <summary>
        /// Creates the appropriate orchestration factory based on the ACH step configuration.
        /// Maps step names/IDs to specific factory implementations.
        /// </summary>
        public IOrchestrationFactory<TResult> CreateFactory<TResult>(ACHStepConfiguration stepConfiguration)
        {
            var agentService = _orchestrationExecutor.CreateAgentService(stepConfiguration);
            var kernelBuilderService = _orchestrationExecutor.GetKernelBuilderService();
            var orchestrationOptions = _orchestrationExecutor.CreateOrchestrationOptions(stepConfiguration);
            var loggerFactory = _orchestrationExecutor.GetLoggerFactory();

            // Map step name or ID to the appropriate factory type
            // This uses a simple name-based matching, but could be enhanced with enum or constants
            var stepName = stepConfiguration.Name.ToLowerInvariant();

            return stepName switch
            {
                "hypothesis brainstorming" or "hypothesisbrainstorming"
                    => CreateTypedFactory<TResult, List<Hypothesis>, HypothesisBrainstormingOrchestrationFactory>(
                        agentService, kernelBuilderService, orchestrationOptions, loggerFactory, _agentResponsePersistence, _tokenUsageExtractor),

                "hypothesis evaluation" or "hypothesisevaluation" or "hypothesis refinement" or "hypothesisrefinement"
                    => CreateTypedFactory<TResult, List<Hypothesis>, HypothesisRefinementOrchestrationFactory>(
                        agentService, kernelBuilderService, orchestrationOptions, loggerFactory, _agentResponsePersistence, _tokenUsageExtractor),

                "evidence extraction" or "evidenceextraction"
                    => CreateTypedFactory<TResult, List<Evidence>, EvidenceExtractionOrchestrationFactory>(
                        agentService, kernelBuilderService, orchestrationOptions, loggerFactory, _agentResponsePersistence, _tokenUsageExtractor),

                "evidence hypothesis evaluation" or "evidencehypothesisevaluation" or "evidence evaluation" or "evidenceevaluation"
                    => CreateTypedFactory<TResult, List<EvidenceHypothesisEvaluation>, EvidenceHypothesisEvaluationOrchestrationFactory>(
                        agentService, kernelBuilderService, orchestrationOptions, loggerFactory, _agentResponsePersistence, _tokenUsageExtractor),

                _ => throw new InvalidOperationException(
                    $"Unknown ACH step name: '{stepConfiguration.Name}'. " +
                    $"Unable to determine the appropriate orchestration factory. " +
                    $"Please check the step configuration or update the factory provider mapping.")
            };
        }

        /// <summary>
        /// Helper method to safely cast factory to the requested result type.
        /// Throws an exception if the types don't match.
        /// </summary>
        private IOrchestrationFactory<TResult> CreateTypedFactory<TResult, TExpectedResult, TFactory>(
            IAgentService agentService,
            IKernelBuilderService kernelBuilderService,
            IOptions<OrchestrationSettings> orchestrationOptions,
            ILoggerFactory loggerFactory,
            IAgentResponsePersistence agentResponsePersistence,
            ITokenUsageExtractor tokenUsageExtractor)
            where TFactory : IOrchestrationFactory<TExpectedResult>
        {
            // Verify that TResult matches TExpectedResult
            if (typeof(TResult) != typeof(TExpectedResult))
            {
                throw new InvalidOperationException(
                    $"Type mismatch: Expected result type {typeof(TExpectedResult).Name} " +
                    $"but requested {typeof(TResult).Name}. " +
                    $"The ACH step configuration may not match the expected factory result type.");
            }

            // Create the factory instance using reflection or direct instantiation
            var factory = (TFactory)Activator.CreateInstance(
                typeof(TFactory),
                agentService,
                kernelBuilderService,
                orchestrationOptions,
                loggerFactory,
                agentResponsePersistence,
                tokenUsageExtractor)!;

            // Cast to the requested interface type
            return (IOrchestrationFactory<TResult>)factory;
        }
    }
}
