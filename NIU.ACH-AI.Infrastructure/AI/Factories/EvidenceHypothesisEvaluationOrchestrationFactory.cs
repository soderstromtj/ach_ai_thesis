using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.Agents.Orchestration;
using Microsoft.SemanticKernel.Agents.Orchestration.Concurrent;
using Microsoft.SemanticKernel.Agents.Orchestration.GroupChat;
using Microsoft.SemanticKernel.Agents.Orchestration.Sequential;
using Microsoft.SemanticKernel.Agents.Orchestration.Transforms;
using Microsoft.SemanticKernel.ChatCompletion;
using NIU.ACH_AI.Application.Configuration;
using NIU.ACH_AI.Application.DTOs;
using NIU.ACH_AI.Application.Interfaces;
using NIU.ACH_AI.Domain.Entities;
using NIU.ACH_AI.Infrastructure.AI.Managers;

namespace NIU.ACH_AI.Infrastructure.AI.Factories
{
#pragma warning disable SKEXP0110 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
    /// <summary>
    /// Factory for creating evidence-hypothesis evaluation orchestrations.
    /// Uses concurrent execution to evaluate evidence against hypotheses.
    /// </summary>
    public class EvidenceHypothesisEvaluationOrchestrationFactory : BaseOrchestrationFactory<EvidenceHypothesisEvaluation, EvidenceHypothesisEvaluation>
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


        protected override AgentOrchestration<string, EvidenceHypothesisEvaluation> CreateOrchestration(
            OrchestrationPromptInput input,
            List<string> agentNames,
            Kernel kernel,
            Agent[] agents,
            StructuredOutputTransform<EvidenceHypothesisEvaluation> outputTransform)
        {
            // Retrieve IChatCompletionService from the kernel's services
            var chatCompletion = kernel.GetRequiredService<IChatCompletionService>();

            // Create the SequentialOrchestration instance
            var orchestration = new SequentialOrchestration<string, EvidenceHypothesisEvaluation>(agents)
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

        protected override EvidenceHypothesisEvaluation UnwrapResult(EvidenceHypothesisEvaluation wrapper)
        {
            return wrapper;
        }

        protected override int GetItemCount(EvidenceHypothesisEvaluation result)
        {
            return 1;
        }

        protected override EvidenceHypothesisEvaluation CreateEmptyResult()
        {
            return new EvidenceHypothesisEvaluation();
        }

        protected override EvidenceHypothesisEvaluation CreateErrorResult()
        {
            return new EvidenceHypothesisEvaluation();
        }

        protected override string GetAgentSelectionReason(string? previousAgentName)
        {
            return "Concurrent execution - all agents run simultaneously";
        }

    }
}
#pragma warning restore SKEXP0110 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
