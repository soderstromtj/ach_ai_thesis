using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.Agents.Orchestration;
using Microsoft.SemanticKernel.Agents.Orchestration.Concurrent;
using Microsoft.SemanticKernel.Agents.Orchestration.Transforms;
using NIU.ACHAI.Application.Configuration;
using NIU.ACHAI.Application.Models;
using NIU.ACHAI.Application.Services;

namespace NIU.ACHAI.Application.Factories
{
#pragma warning disable SKEXP0110 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
    /// <summary>
    /// Example factory demonstrating how to use ConcurrentOrchestration with the refactored BaseOrchestrationFactory.
    /// ConcurrentOrchestration does not require a GroupChatManager - it only needs agents.
    /// </summary>
    public class HypothesisBrainstormingOrchestrationFactory : BaseOrchestrationFactory<List<Hypothesis>, HypothesisResult>
    {
        public HypothesisBrainstormingOrchestrationFactory(
            IAgentService agentService,
            IKernelBuilderService kernelBuilderService,
            IOptions<OrchestrationSettings> orchestrationSettings,
            ILoggerFactory loggerFactory)
            : base(agentService, kernelBuilderService, orchestrationSettings, loggerFactory)
        {
        }

        protected override ILogger CreateLogger(ILoggerFactory loggerFactory)
        {
            return loggerFactory.CreateLogger<HypothesisBrainstormingOrchestrationFactory>();
        }

        protected override AgentOrchestration<string, HypothesisResult> CreateOrchestration(
            OrchestrationPromptInput input,
            List<string> agentNames,
            Kernel kernel,
            Agent[] agents,
            StructuredOutputTransform<HypothesisResult> outputTransform)
        {
            // All agents execute concurrently and their results are aggregated.
            return new ConcurrentOrchestration<string, HypothesisResult>(agents)
            {
                ResponseCallback = ResponseCallback,
                ResultTransform = outputTransform.TransformAsync,
                StreamingResponseCallback = StreamingResponseCallback,
            };
        }

        protected override string GetResultTypeName()
        {
            return nameof(HypothesisResult);
        }

        protected override List<Hypothesis> UnwrapResult(HypothesisResult wrapper)
        {
            return wrapper.Hypotheses;
        }

        protected override int GetItemCount(List<Hypothesis> result)
        {
            return result.Count;
        }

        protected override List<Hypothesis> CreateEmptyResult()
        {
            return new List<Hypothesis>();
        }

        protected override List<Hypothesis> CreateErrorResult()
        {
            return new List<Hypothesis>();
        }

        protected override string GetAgentSelectionReason(string? previousAgentName)
        {
            return "Concurrent execution - all agents run simultaneously";
        }
    }
}
#pragma warning restore SKEXP0110 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
