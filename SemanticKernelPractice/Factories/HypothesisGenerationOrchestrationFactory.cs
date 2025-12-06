using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents.Orchestration.GroupChat;
using Microsoft.SemanticKernel.ChatCompletion;
using SemanticKernelPractice.Configuration;
using SemanticKernelPractice.Managers;
using SemanticKernelPractice.Models;
using SemanticKernelPractice.Services;

namespace SemanticKernelPractice.Factories
{
#pragma warning disable SKEXP0110 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
    public class HypothesisGenerationOrchestrationFactory : BaseOrchestrationFactory<List<Hypothesis>, HypothesisResult>
    {
        public HypothesisGenerationOrchestrationFactory(
            IAgentService agentService,
            IKernelBuilderService kernelBuilderService,
            IOptions<OrchestrationSettings> orchestrationSettings,
            ILoggerFactory loggerFactory)
            : base(agentService, kernelBuilderService, orchestrationSettings, loggerFactory)
        {
        }

        protected override ILogger CreateLogger(ILoggerFactory loggerFactory)
        {
            return loggerFactory.CreateLogger<HypothesisGenerationOrchestrationFactory>();
        }

        protected override GroupChatManager CreateManager(OrchestrationPromptInput input, List<string> agentNames, Kernel kernel)
        {
            return new HypothesisGenerationGroupChatManager(
                input,
                agentNames,
                kernel.GetRequiredService<IChatCompletionService>(),
                new HypothesisGenerationPromptStrategy(),
                new AgentParticipationTracker(),
                _loggerFactory.CreateLogger<HypothesisGenerationGroupChatManager>());
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
                    Description = "An error occurred during the orchestration process"
                }
            };
        }

        protected override string GetAgentSelectionReason(string? previousAgentName)
        {
            return $"{nameof(HypothesisGenerationGroupChatManager)} selection after {previousAgentName}";
        }
    }
}
#pragma warning restore SKEXP0110 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
