namespace NIU.ACH_AI.Application.Configuration
{
    /// <summary>
    /// Enumerates the supported external artificial intelligence platforms.
    /// </summary>
    /// <remarks>
    /// Acts as a business-level toggle to determine which underlying API ecosystem fulfills LLM requests during operations.
    /// </remarks>
    public enum AIServiceProvider
    {
        AzureOpenAI,
        OpenAI,
        Ollama,
        HuggingFace,
        Unified,
    }
}
