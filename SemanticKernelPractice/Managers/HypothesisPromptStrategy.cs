using SemanticKernelPractice.Models;

namespace SemanticKernelPractice.Managers
{
    /// <summary>
    /// Default implementation of <see cref="IGroupChatPromptStrategy"/> for hypothesis generation workflows.
    /// </summary>
    public class HypothesisPromptStrategy : IGroupChatPromptStrategy
    {
        /// <inheritdoc/>
        public string GetTerminationPrompt(OrchestrationPromptInput input, List<string> agentNames) =>
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

        /// <inheritdoc/>
        public string GetSelectionPrompt(OrchestrationPromptInput input, List<string> agentNames, int turnCount, int maxInvocationLimit) =>
            $"""
            You are the group chat manager for a team of expert analysts tasked with generating hypotheses on the following key question: "{input.KeyQuestion}".
            This process is step 1 of a larger workflow using the Analysis of Competing Hypotheses (ACH) framework developed by Richards Heuer.
            Your job is to select the next agent to contribute to the discussion.
            The analysts are named: {string.Join(", ", agentNames)}.

            The current turn count is {turnCount} and the maximum amount of turns is {maxInvocationLimit}. Please select the next agent to contribute, ensuring that all agents have an opportunity to participate.

            Respond with only the name of the selected agent. For example, if you select "{agentNames[0]}", respond only with: {agentNames[0]}.

            Do not add any additional commentary or reasoning.
            """;

        /// <inheritdoc/>
        public string GetFilterPrompt(OrchestrationPromptInput input) =>
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
}
