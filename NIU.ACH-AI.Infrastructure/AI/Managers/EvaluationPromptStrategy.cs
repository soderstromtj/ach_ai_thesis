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
            You are the group chat manager for a team of expert analysts tasked with evaluating evidence against a hypothesis, which is part of step 3 of the Analysis of Competing Hypotheses (ACH) framework developed by Richards Heuer.

            The available agents are:
            {string.Join("\n- ", agentNames)}

            This discussion has 3 phases:
            - Phase 1: Ensure all DIME (Diplomatic, Information, Military, and Economic) agents have contributed at least once.
            - Phase 2: Once all DIME agents have contributed, the next agent must be the Reviewer agent.
            - Phase 3: After the Reviewer agent has contributed, the Summarizer agent must consolidate the DIME and Deception agents' evaluations into one, coherent analysis.

            Please select the next agent to contribute, and respond with only the name of the selected agent. For example, if you select "{agentNames.First()}", respond only with: {agentNames.First()}.

            Do not add any additional commentary or reasoning.
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
