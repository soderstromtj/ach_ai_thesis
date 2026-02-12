using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.Agents.Orchestration;
using Microsoft.SemanticKernel.Agents.Orchestration.GroupChat;
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
    /// Factory for creating evidence extraction orchestrations.
    /// Uses group chat execution to extract evidence from multiple sources/agents collaboratively.
    /// </summary>
    public class EvidenceExtractionOrchestrationFactory : BaseOrchestrationFactory<List<Evidence>, EvidenceResult>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EvidenceExtractionOrchestrationFactory"/> class.
        /// </summary>
        /// <param name="agentService">Service for creating agents.</param>
        /// <param name="kernelBuilderService">Service for building semantic kernels.</param>
        /// <param name="orchestrationSettings">Settings for orchestration execution.</param>
        /// <param name="loggerFactory">Logger factory.</param>
        /// <param name="agentResponsePersistence">Optional service for persisting agent responses.</param>
        /// <param name="tokenUsageExtractor">Optional service for extracting token usage.</param>
        public EvidenceExtractionOrchestrationFactory(
            IAgentService agentService,
            IKernelBuilderService kernelBuilderService,
            IOptions<OrchestrationSettings> orchestrationSettings,
            ILoggerFactory loggerFactory,
            IAgentResponsePersistence? agentResponsePersistence = null)
            : base(agentService, kernelBuilderService, orchestrationSettings, loggerFactory, agentResponsePersistence)
        {
        }

        protected override AgentOrchestration<string, EvidenceResult> CreateOrchestration(
            OrchestrationPromptInput input,
            Kernel kernel,
            Agent[] agents,
            StructuredOutputTransform<EvidenceResult> outputTransform)
        {
            // Retrieve IChatCompletionService from the kernel's services
            var chatCompletion = kernel.GetRequiredService<IChatCompletionService>();

            var agent_names = agents.Select(agent => agent.Name!).ToList();

            EvidenceExtractionGroupChatManager groupChatManager =
                new EvidenceExtractionGroupChatManager(
                    input,
                    agent_names,
                    chatCompletion,
                    new EvidenceExtractionPromptStrategy(),
                    new AgentParticipationTracker(),
                    this._loggerFactory.CreateLogger<EvidenceExtractionGroupChatManager>());

            // Create the GroupChatOrchestration instance
            var orchestration = new GroupChatOrchestration<string, EvidenceResult>(groupChatManager, agents)
            {
                ResponseCallback = ResponseCallback,
                ResultTransform = outputTransform.TransformAsync,
                StreamingResponseCallback = StreamingResponseCallback,
            };

            return orchestration;
        }

        protected override string GetResultTypeName()
        {
            return nameof(EvidenceResult);
        }

        protected override List<Evidence> UnwrapResult(EvidenceResult wrapper)
        {
            return wrapper.Evidence;
        }

        protected override int GetItemCount(List<Evidence> result)
        {
            return result.Count;
        }

        protected override List<Evidence> CreateEmptyResult()
        {
            return new List<Evidence>();
        }

        protected override List<Evidence> CreateErrorResult()
        {
            return new List<Evidence>();
        }

        protected override string GetAgentSelectionReason(string? previousAgentName)
        {
            return "Group chat execution - agents selected by manager";
        }
    }
}
#pragma warning restore SKEXP0110 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
