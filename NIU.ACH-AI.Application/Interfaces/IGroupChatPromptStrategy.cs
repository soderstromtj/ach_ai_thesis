using NIU.ACH_AI.Application.DTOs;

namespace NIU.ACH_AI.Application.Interfaces
{
    /// <summary>
    /// Defines the contract for generating prompts used in group chat orchestration.
    /// </summary>
    public interface IGroupChatPromptStrategy
    {
        /// <summary>
        /// Generates a prompt to determine if the group chat should terminate.
        /// </summary>
        /// <param name="input">The orchestration prompt input containing the key question and context.</param>
        /// <param name="agentNames">The names of all agents participating in the chat.</param>
        /// <returns>A prompt string for the termination decision.</returns>
        string GetTerminationPrompt(OrchestrationPromptInput input, List<string> agentNames);

        /// <summary>
        /// Generates a prompt to select the next agent to contribute.
        /// </summary>
        /// <param name="input">The orchestration prompt input containing the key question and context.</param>
        /// <param name="agentNames">The names of all agents participating in the chat.</param>
        /// <returns>A prompt string for agent selection.</returns>
        string GetSelectionPrompt(OrchestrationPromptInput input, List<string> agentNames);

        /// <summary>
        /// Generates a prompt to filter and format the results from the group chat.
        /// </summary>
        /// <param name="input">The orchestration prompt input containing the key question and context.</param>
        /// <returns>A prompt string for filtering results.</returns>
        string GetFilterPrompt(OrchestrationPromptInput input);
    }
}
