namespace NIU.ACH_AI.Application.Configuration
{
    /// <summary>
    /// Defines the available AI service providers that can be used for ACH steps.
    /// This is a business-level configuration that determines which AI service to use.
    /// </summary>
    public enum AIServiceProvider
    {
        AzureOpenAI,
        OpenAI,
        Ollama,
        HuggingFace,
        Unified,
    }
}
