using NIU.ACH_AI.Application.DTOs;

namespace NIU.ACH_AI.Application.Interfaces
{
    /// <summary>
    /// Defines the contract for generating structural prompts required by the chat orchestration engine.
    /// </summary>
    public interface IGroupChatPromptStrategy
    {
        /// <summary>
        /// Generates the system prompt used to determine if the conversation has reached a natural conclusion.
        /// </summary>
        /// <param name="input">The orchestration prompt input containing the key question and context.</param>
        /// <param name="agentNames">The names of all agents participating in the chat.</param>
        /// <returns>A prompt string for the termination decision.</returns>
        string GetTerminationPrompt(OrchestrationPromptInput input, IEnumerable<string> agentNames);

        /// <summary>
        /// Generates a prompt to select the next agent to contribute.
        /// </summary>
        /// <param name="input">The payload containing the active analytical question and context.</param>
        /// <param name="agentNames">The active roster of actors capable of contributing.</param>
        /// <returns>The fully formatted selection logic prompt.</returns>
        string GetSelectionPrompt(OrchestrationPromptInput input, IEnumerable<string> agentNames);

        /// <summary>
        /// Generates the system prompt used to parse and extract the final agreed-upon result from the conversation log.
        /// </summary>
        /// <param name="input">The payload containing the active analytical question and context.</param>
        /// <returns>The fully formatted extraction prompt.</returns>
        string GetFilterPrompt(OrchestrationPromptInput input);
    }
}
