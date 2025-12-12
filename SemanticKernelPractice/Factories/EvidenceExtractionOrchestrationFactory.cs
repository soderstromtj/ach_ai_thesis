using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.Agents.Orchestration;
using Microsoft.SemanticKernel.Agents.Orchestration.GroupChat;
using Microsoft.SemanticKernel.Agents.Orchestration.Transforms;
using SemanticKernelPractice.Configuration;
using SemanticKernelPractice.Managers;
using SemanticKernelPractice.Models;
using SemanticKernelPractice.Services;

namespace SemanticKernelPractice.Factories
{
#pragma warning disable SKEXP0110 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
    public class EvidenceExtractionOrchestrationFactory : BaseOrchestrationFactory<List<Evidence>, EvidenceResult>
    {
        public EvidenceExtractionOrchestrationFactory(
            IAgentService agentService,
            IKernelBuilderService kernelBuilderService,
            IOptions<OrchestrationSettings> orchestrationSettings,
            ILoggerFactory loggerFactory)
            : base(agentService, kernelBuilderService, orchestrationSettings, loggerFactory)
        {
        }

        protected override ILogger CreateLogger(ILoggerFactory loggerFactory)
        {
            return loggerFactory.CreateLogger<EvidenceExtractionOrchestrationFactory>();
        }

        protected override AgentOrchestration<string, EvidenceResult> CreateOrchestration(
            OrchestrationPromptInput input,
            List<string> agentNames,
            Kernel kernel,
            Agent[] agents,
            StructuredOutputTransform<EvidenceResult> outputTransform)
        {
            // Create round-robin manager for evidence extraction
            var manager = new RoundRobinGroupChatManager();

            // Create and return GroupChatOrchestration
            return new GroupChatOrchestration<string, EvidenceResult>(manager, agents)
            {
                ResponseCallback = ResponseCallback,
                ResultTransform = outputTransform.TransformAsync,
                StreamingResponseCallback = StreamingResponseCallback,
            };
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
            return new List<Evidence>
            {
                new Evidence
                {
                    Id = -1,
                    Description = "Error during orchestration",
                    Type = EvidenceType.Fact
                }
            };
        }

        protected override string GetAgentSelectionReason(string? previousAgentName)
        {
            return $"Round-robin selection after {previousAgentName}";
        }
    }
}
#pragma warning restore SKEXP0110 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
