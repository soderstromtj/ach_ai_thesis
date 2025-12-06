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
                You are the group chat manager for a team of expert analysts tasked with generating hypotheses on the following key question: "{input.KeyQuestion}".
                This process is step 1 of a larger workflow using the Analysis of Competing Hypotheses (ACH) framework developed by Richards Heuer.
                Your job is to determine whether the current list of hypotheses is sufficient or or if discussion should continue.

                You must ensure the following criteria are met before deciding to end the discussion:
                - Each agent has had a chance to contribute at least once. The agents are: {string.Join(", ", agentNames)}.
                - The hypotheses are mutually exclusive and collectively exhaustive.
                - The hypotheses are relevant to the key question.

                Your response must be either "True" to end the discussion or "False" to continue.
                """;

            public static string Selection(OrchestrationPromptInput input, List<string> agentNames, int turnCount, int maxInvocationLimit) =>
                $"""
                You are the group chat manager for a team of expert analysts tasked with generating hypotheses on the following key question: "{input.KeyQuestion}".
                This process is step 1 of a larger workflow using the Analysis of Competing Hypotheses (ACH) framework developed by Richards Heuer.
                Your job is to select the next agent to contribute to the discussion.
                The analysts are named: {string.Join(", ", agentNames)}.

                The current turn count is {turnCount} and the maximum amount of turns is {maxInvocationLimit}. Please select the next agent to contribute, ensuring that all agents have an opportunity to participate.

                Respond with only the name of the selected agent. For example, if you select "{agentNames[0]}", respond only with: {agentNames[0]}.
                
                Do not add any additional commentary or reasoning.
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

        public override async ValueTask<GroupChatManagerResult<bool>> ShouldTerminate(ChatHistory history, CancellationToken cancellationToken = default)
        {
            
            if (maximumInvocationLimit > 0 && _turnCount >= maximumInvocationLimit)
            {
                return new GroupChatManagerResult<bool>(true)
                {
                    Reason = $"Maximum invocation limit of {maximumInvocationLimit} reached."
                };
            }

            // Determine if all agents have contributed at least once
            var participatingAgents = new HashSet<string>(
                history
                .Where(msg => msg.Role == AuthorRole.Assistant)
                .Select(msg => msg.AuthorName ?? string.Empty)
                .Where(name => !string.IsNullOrWhiteSpace(name))
                );

            if (participatingAgents.Count < agentNames.Count)
            {
                return new GroupChatManagerResult<bool>(false)
                {
                    Reason = "Not all agents have contributed at least once."
                };
            }

            var responseTask = await this.GetResponseAsync<bool>(history, Prompts.Termination(input, agentNames), cancellationToken);

            return new GroupChatManagerResult<bool>(responseTask.Value)
            {
                Reason = "Determined by group chat manager prompt response."
            };
        }

        #region Private Methods
        private async ValueTask<GroupChatManagerResult<TValue>> GetResponseAsync<TValue>(ChatHistory history, string prompt, CancellationToken cancellationToken = default)
        {
            OpenAIPromptExecutionSettings executionSettings = new() { ResponseFormat = typeof(GroupChatManagerResult<TValue>) };

            ChatHistory request = [.. history, new ChatMessageContent(AuthorRole.System, prompt)];
            var response = await chatCompletion.GetChatMessageContentsAsync(request, executionSettings, kernel: null, cancellationToken);
            string responseText = response.FirstOrDefault()?.ToString() ?? string.Empty;

            var result = JsonSerializer.Deserialize<GroupChatManagerResult<TValue>>(responseText) ??
                throw new InvalidOperationException($"Failed to parse response: {responseText}");

            return result;                
        }

        #endregion
    }
#pragma warning restore SKEXP0110 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
}
