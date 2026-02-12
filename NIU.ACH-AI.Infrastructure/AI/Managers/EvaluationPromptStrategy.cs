using NIU.ACH_AI.Application.DTOs;
using NIU.ACH_AI.Application.Interfaces;

namespace NIU.ACH_AI.Infrastructure.AI.Managers
{
    /// <summary>
    /// Default implementation of <see cref="IGroupChatPromptStrategy"/> for evidence v. hypothesis workflows.
    /// </summary>
    public class EvaluationPromptStrategy : IGroupChatPromptStrategy
    {
        /// <inheritdoc/>
        public string GetTerminationPrompt(OrchestrationPromptInput input, IEnumerable<string> agentNames) =>
            $"""
            You are the group chat manager for a team of expert analysts tasked with evaluating evidence against a hypothesis, which is part of step 3 of the Analysis of Competing Hypotheses (ACH) framework developed by Richards Heuer.

            You must ensure the following criteria are met before deciding to end the discussion:
            - Each agent has had a chance to contribute at least once. The agents are: {string.Join(", ", agentNames)}.
            - The Reviewer agent has reviewed all contributions and made necessary corrections.
            - The Summarizer agent has consolidated a final evaluation in proper JSON format.

            Your response must be either "True" to end the discussion or "False" to continue.
            """;

        /// <inheritdoc/>
        public string GetSelectionPrompt(OrchestrationPromptInput input, IEnumerable<string> agentNames) =>
            $"""
            You are the group chat manager for an ACH analysis team. Your goal is to manage the flow of conversation based on the Strict Transition Rules below.

            The available agents are:
            {string.Join("\n- ", agentNames)}

            *** STRICT TRANSITION RULES (Evaluate in Order) ***

            1. CHECK LAST SPEAKER:
               - Look at the MOST RECENT message in the chat history.
               - IF the last speaker was 'ReviewerAgent' -> You MUST select 'SummarizerAgent'. (Do not select ReviewerAgent twice).
               - IF the last speaker was 'SummarizerAgent' -> The process is complete.

            2. CHECK DIME COMPLETION:
               - IF the Reviewer has NOT spoken yet, check the DIME agents (Diplomatic, Information, Military, Economic).
               - Select any DIME agent that has NOT yet contributed.
               - IF all DIME agents have contributed, select 'ReviewerAgent'.

            Response Requirement:
            Respond ONLY with the exact name of the selected agent. Do not provide reasoning.
            """;

        /// <inheritdoc/>
        public string GetFilterPrompt(OrchestrationPromptInput input) =>
            $$$"""
            You are the group chat manager for a team of expert agents tasked with evaluating evidence against a hypothesis, which is part of step 3 of the Analysis of Competing Hypotheses (ACH) framework developed by Richards Heuer.

            Your job is to review the most current list of hypotheses and organize it into a JSON object with the following example structure:

            {{
              "Score": "Consistent",
              "ScoreRationale": "A comprehensive and consolidated analysis of the evaluations from all of the DIME agents' evaluations",
              "ConfidenceLevel": 0.85,
              "ConfidenceRationale": "A comprehensive and well-reasoned rationale of the confidence based on the evaluations"
            }}

            You must ensure to only respond with the JSON object and no additional commentary or reasoning.
            """;
    }
}
