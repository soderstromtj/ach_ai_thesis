using NIU.ACH_AI.Application.DTOs;

namespace NIU.ACH_AI.Application.Interfaces
{
    /// <summary>
    /// Service responsible for formatting an OrchestrationPromptInput into a string for LLMs.
    /// </summary>
    public interface IOrchestrationPromptFormatter
    {
        /// <summary>
        /// Formats the prompt input into a human-readable string.
        /// </summary>
        string FormatPrompt(OrchestrationPromptInput input);
    }
}
