using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.Agents.Orchestration;
using Microsoft.SemanticKernel.Agents.Orchestration.GroupChat;
using Microsoft.SemanticKernel.Agents.Orchestration.Sequential;
using Microsoft.SemanticKernel.Agents.Orchestration.Transforms;
using Microsoft.SemanticKernel.ChatCompletion;
using NIU.ACHAI.Application.Configuration;
using NIU.ACHAI.Application.Managers;
using NIU.ACHAI.Application.Models;
using NIU.ACHAI.Application.Services;

namespace NIU.ACHAI.Application.Factories
{
#pragma warning disable SKEXP0110 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
    public class HypothesisRefinementOrchestrationFactory : BaseOrchestrationFactory<List<Hypothesis>, HypothesisResult>
    {
        public HypothesisRefinementOrchestrationFactory(
            IAgentService agentService,
            IKernelBuilderService kernelBuilderService,
            IOptions<OrchestrationSettings> orchestrationSettings,
            ILoggerFactory loggerFactory)
            : base(agentService, kernelBuilderService, orchestrationSettings, loggerFactory)
        {
        }

        protected override ILogger CreateLogger(ILoggerFactory loggerFactory)
        {
            return loggerFactory.CreateLogger<HypothesisRefinementOrchestrationFactory>();
        }

        protected override AgentOrchestration<string, HypothesisResult> CreateOrchestration(
            OrchestrationPromptInput input,
            List<string> agentNames,
            Kernel kernel,
            Agent[] agents,
            StructuredOutputTransform<HypothesisResult> outputTransform)
        {
            // Create and return a SequentialOrchestration instance
            SequentialOrchestration<string, HypothesisResult> orchestration = new SequentialOrchestration<string, HypothesisResult>(agents)
            {
                ResponseCallback = ResponseCallback,
                ResultTransform = outputTransform.TransformAsync,
                StreamingResponseCallback = StreamingResponseCallback,
            };

            return orchestration;
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
            return new List<Hypothesis>
            {
                new Hypothesis
                {
                    Title = "Error during orchestration",
                    Rationale = "An error occurred during the orchestration process"
                }
            };
        }

        protected override string GetAgentSelectionReason(string? previousAgentName)
        {
            return $"Sequential selection after {previousAgentName}";
        }
    }
}
#pragma warning restore SKEXP0110 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
