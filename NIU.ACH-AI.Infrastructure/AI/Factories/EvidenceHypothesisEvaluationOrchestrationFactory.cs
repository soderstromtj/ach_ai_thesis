using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.Agents.Orchestration;
using Microsoft.SemanticKernel.Agents.Orchestration.Concurrent;
using Microsoft.SemanticKernel.Agents.Orchestration.Transforms;
using NIU.ACH_AI.Application.Configuration;
using NIU.ACH_AI.Application.DTOs;
using NIU.ACH_AI.Application.Interfaces;
using NIU.ACH_AI.Domain.Entities;

namespace NIU.ACH_AI.Infrastructure.AI.Factories
{
#pragma warning disable SKEXP0110 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
    /// <summary>
    /// Factory for creating evidence-hypothesis evaluation orchestrations.
    /// Uses concurrent execution to evaluate evidence against hypotheses.
    /// </summary>
    public class EvidenceHypothesisEvaluationOrchestrationFactory : BaseOrchestrationFactory<List<EvidenceHypothesisEvaluation>, EvidenceHypothesisEvaluationResult>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EvidenceHypothesisEvaluationOrchestrationFactory"/> class.
        /// </summary>
        /// <param name="agentService">Service for creating agents.</param>
        /// <param name="kernelBuilderService">Service for building semantic kernels.</param>
        /// <param name="orchestrationSettings">Settings for orchestration execution.</param>
        /// <param name="loggerFactory">Logger factory.</param>
        /// <param name="agentResponsePersistence">Optional service for persisting agent responses.</param>
        /// <param name="tokenUsageExtractor">Optional service for extracting token usage.</param>
        public EvidenceHypothesisEvaluationOrchestrationFactory(
            IAgentService agentService,
            IKernelBuilderService kernelBuilderService,
            IOptions<OrchestrationSettings> orchestrationSettings,
            ILoggerFactory loggerFactory,
            IAgentResponsePersistence? agentResponsePersistence = null)
            : base(agentService, kernelBuilderService, orchestrationSettings, loggerFactory, agentResponsePersistence)
        {
        }


        protected override AgentOrchestration<string, EvidenceHypothesisEvaluationResult> CreateOrchestration(
            OrchestrationPromptInput input,
            List<string> agentNames,
            Kernel kernel,
            Agent[] agents,
            StructuredOutputTransform<EvidenceHypothesisEvaluationResult> outputTransform)
        {
            // Create the ConcurrentOrchestration instance
            var orchestration = new ConcurrentOrchestration<string, EvidenceHypothesisEvaluationResult>(agents)
            {
                ResponseCallback = ResponseCallback,
                ResultTransform = outputTransform.TransformAsync,
                StreamingResponseCallback = StreamingResponseCallback,
            };

            return orchestration;
        }

        protected override string GetResultTypeName()
        {
            return nameof(EvidenceHypothesisEvaluationResult);
        }

        protected override List<EvidenceHypothesisEvaluation> UnwrapResult(EvidenceHypothesisEvaluationResult wrapper)
        {
            return wrapper.Evaluations;
        }

        protected override int GetItemCount(List<EvidenceHypothesisEvaluation> result)
        {
            return result.Count;
        }

        protected override List<EvidenceHypothesisEvaluation> CreateEmptyResult()
        {
            return new List<EvidenceHypothesisEvaluation>();
        }

        protected override List<EvidenceHypothesisEvaluation> CreateErrorResult()
        {
            return new List<EvidenceHypothesisEvaluation>();
        }

        protected override string GetAgentSelectionReason(string? previousAgentName)
        {
            return "Concurrent execution - all agents run simultaneously";
        }

    }
}
#pragma warning restore SKEXP0110 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
