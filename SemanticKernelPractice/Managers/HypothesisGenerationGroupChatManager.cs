using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents.Orchestration.GroupChat;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using SemanticKernelPractice.Models;
using SemanticKernelPractice.Services;
using System.Text.Json;

namespace SemanticKernelPractice.Managers
{
#pragma warning disable SKEXP0110 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
    public class HypothesisGenerationGroupChatManager(
        OrchestrationPromptInput input, 
        List<string> agentNames, 
        int maximumInvocationLimit, 
        IChatCompletionService chatCompletion
        ) : GroupChatManager
    {
        private static class Prompts
        {
            public static string Termination(OrchestrationPromptInput input, List<string> agentNames) =>
                $"""
                You are the group chat manager for a team of expert agents tasked with generating hypotheses on the following key question: "{input.KeyQuestion}".
                This process is step 1 of a larger workflow using the Analysis of Competing Hypotheses (ACH) framework developed by Richards Heuer.
                Your job is to determine whether the current list of hypotheses is sufficient, if more hypotheses should be generated, or if discussion should continue.

                You must ensure the following criteria are met before deciding to end the discussion:
                - Each agent has had a chance to contribute at least once. The agents are: {string.Join(", ", agentNames)}.
                - The hypotheses are mutually exclusive and collectively exhaustive.
                - The hypotheses are relevant to the key question.

                If you would like to end the discussion, please repond with "True". Otherwise, respond with "False". Do not add any additional commentary or reasoning.
                """;

            public static string Selection(OrchestrationPromptInput input, List<string> agentNames, int turnCount, int maxInvocationLimit) =>
                $"""
                You are the group chat manager for a team of expert agents tasked with generating hypotheses on the following key question: "{input.KeyQuestion}".
                This process is step 1 of a larger workflow using the Analysis of Competing Hypotheses (ACH) framework developed by Richards Heuer.
                Your job is to select the next agent to contribute to the discussion.
                The agents are: {string.Join(", ", agentNames)}.
                The current turn count is {turnCount} and the maximum amount of turns is {maxInvocationLimit}. Please select the next agent to contribute, ensuring that all agents have an opportunity to participate.
                Respond with only the name of the selected agent. Do not add any additional commentary or reasoning.
                """;

            public static string Filter(OrchestrationPromptInput input) =>
                $$$"""
                You are the group chat manager for a team of expert agents tasked with generating hypotheses on the following key question: "{{{input.KeyQuestion}}}".
                This process is step 1 of a larger workflow using the Analysis of Competing Hypotheses (ACH) framework developed by Richards Heuer.
                Your job is to review the most current list of hypotheses and organize it into a JSON object with the following structure:
                {{"Hypotheses": [
                        {{ "Title": "Hypothesis 1", "Rationale": "" }},
                        {{ "Title": "Hypothesis 2", "Rationale": "" }}
                    ]}}

                You must ensure to only respond with the JSON object and no additional commentary or reasoning.
                """;

        }

        // Count how many times we have selected an agent (a rough "turn" count).
        private int _turnCount = 0;

        public override ValueTask<GroupChatManagerResult<string>> FilterResults(ChatHistory history, CancellationToken cancellationToken = default)
        {
            
            return this.GetResponseAsync<string>(history, Prompts.Filter(input), cancellationToken);
        }

        public override ValueTask<GroupChatManagerResult<string>> SelectNextAgent(ChatHistory history, GroupChatTeam team, CancellationToken cancellationToken = default)
        {
            return this.GetResponseAsync<string>(history, Prompts.Selection(input, agentNames, ++_turnCount, maximumInvocationLimit), cancellationToken);
        }

        public override ValueTask<GroupChatManagerResult<bool>> ShouldRequestUserInput(ChatHistory history, CancellationToken cancellationToken = default)
        {
            return ValueTask.FromResult(
                new GroupChatManagerResult<bool>(false)
                {
                    Reason = "Automated ACH evidence extraction workflow - no user input needed."
                });
        }

        public override ValueTask<GroupChatManagerResult<bool>> ShouldTerminate(ChatHistory history, CancellationToken cancellationToken = default)
        {
            

            return ValueTask.FromResult(
                new GroupChatManagerResult<bool>(false)
                {
                    Reason = "Continue conversation to further refine the ACH Evidence list."
                });
        }

        #region Private Methods
        private async ValueTask<GroupChatManagerResult<TValue>> GetResponseAsync<TValue>(ChatHistory history, string prompt, CancellationToken cancellationToken = default)
        {
            OpenAIPromptExecutionSettings executionSettings = new() { ResponseFormat = typeof(GroupChatManagerResult<TValue>) };

            ChatHistory request = [.. history, new ChatMessageContent(AuthorRole.System, prompt)];
            var response = await chatCompletion.GetChatMessageContentsAsync(request, executionSettings, kernel: null, cancellationToken);
            string responseText = response.FirstOrDefault()?.ToString() ?? string.Empty;

            return
                JsonSerializer.Deserialize<GroupChatManagerResult<TValue>>(responseText) ??
                throw new InvalidOperationException($"Failed to parse response: {responseText}");
        }

        #endregion
    }
#pragma warning restore SKEXP0110 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
}
