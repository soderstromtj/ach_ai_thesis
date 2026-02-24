using NIU.ACH_AI.Application.DTOs;
using NIU.ACH_AI.Application.Interfaces;

namespace NIU.ACH_AI.Infrastructure.AI.Managers
{
    /// <summary>
    /// Provides the prompt templates used by the manager for the evidence extraction step.
    /// </summary>
    public class EvidenceExtractionPromptStrategy : IGroupChatPromptStrategy
    {
        public string GetTerminationPrompt(OrchestrationPromptInput input, IEnumerable<string> agentNames) =>
            $"""
            You are the group chat manager for a team of expert analysts tasked with extracting evidence from a provided context, which is Step 2 of the Analysis of Competing Hypotheses (ACH) framework.

            You must ensure the following criteria are met before deciding to end the discussion:
            - Each "Extractor" agent (Diplomatic, Informational, Military, Economic, Deception) has had a chance to contribute at least once.
            - The Reviewer agent has reviewed the extracted evidence for duplicates and quality issues.
            - The Deduplication agent has consolidated the final list of evidence.

            Your response must be either "True" to end the discussion or "False" to continue.
            """;

        public string GetSelectionPrompt(OrchestrationPromptInput input, IEnumerable<string> agentNames) =>
            $"""
            You are the group chat manager for a team of expert analysts tasked with extracting evidence from a provided context, which is Step 2 of the Analysis of Competing Hypotheses (ACH) framework.

            The available agents are:
            {string.Join("\n- ", agentNames)}

            This discussion has 3 phases:
            - Phase 1: Ensure all Extractor agents (Diplomatic, Informational, Military, Economic, Deception) have contributed at least once to extract raw evidence from the context.
            - Phase 2: Once all Extractor agents have contributed, the Reviewer agent must review the current list for duplicates, quality, and consistency.
            - Phase 3: After the Reviewer agent has completed their review, the Deduplication agent must consolidate the evidence into a final, clean list.

            Please select the next agent to contribute, and respond with only the name of the selected agent. For example, if you select "{agentNames.First()}", respond only with: {agentNames.First()}.

            Do not add any additional commentary or reasoning.
            """;

        public string GetFilterPrompt(OrchestrationPromptInput input) =>
            $$$"""
            You are the group chat manager for a team of expert analysts tasked with extracting evidence from a provided context.

            Your job is to review the most current list of evidence and organize it into a JSON object with the following example structure:

            {
              "Evidence": [
                {
                  "EvidenceId": "guid",
                  "Claim": "The text states...",
                  "ReferenceSnippet": "Quoted text...",
                  "Type": "Fact",
                  "Notes": "Source reliability..."
                }
              ]
            }

            You must ensure to only respond with the JSON object and no additional commentary or reasoning.
            """;
    }
}
