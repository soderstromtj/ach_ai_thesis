using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.Agents.Orchestration;
using Microsoft.SemanticKernel.Agents.Orchestration.Concurrent;
using Microsoft.SemanticKernel.Agents.Orchestration.Transforms;
using SemanticKernelPractice.Configuration;
using SemanticKernelPractice.Models;
using SemanticKernelPractice.Services;

namespace SemanticKernelPractice.Factories
{
#pragma warning disable SKEXP0110 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
    /// <summary>
    /// Example factory demonstrating how to use ConcurrentOrchestration with the refactored BaseOrchestrationFactory.
    /// ConcurrentOrchestration does not require a GroupChatManager - it only needs agents.
    /// </summary>
    public class ExampleConcurrentOrchestrationFactory : BaseOrchestrationFactory<List<Evidence>, EvidenceResult>
    {
        public ExampleConcurrentOrchestrationFactory(
            IAgentService agentService,
            IKernelBuilderService kernelBuilderService,
            IOptions<OrchestrationSettings> orchestrationSettings,
            ILoggerFactory loggerFactory)
            : base(agentService, kernelBuilderService, orchestrationSettings, loggerFactory)
        {
        }

        protected override ILogger CreateLogger(ILoggerFactory loggerFactory)
        {
            return loggerFactory.CreateLogger<ExampleConcurrentOrchestrationFactory>();
        }

        protected override AgentOrchestration<string, EvidenceResult> CreateOrchestration(
            OrchestrationPromptInput input,
            List<string> agentNames,
            Kernel kernel,
            Agent[] agents,
            StructuredOutputTransform<EvidenceResult> outputTransform)
        {
            // ConcurrentOrchestration only needs agents - no manager required!
            // All agents execute concurrently and their results are aggregated.
            return new ConcurrentOrchestration<string, EvidenceResult>(agents)
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
            return "Concurrent execution - all agents run simultaneously";
        }
    }
}
#pragma warning restore SKEXP0110 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
