namespace SemanticKernelPractice.Configuration
{
    /// <summary>
    /// Global AI service provider configurations.
    /// The Provider selection is now per-experiment in ExperimentAIServiceSettings.
    /// </summary>
    public class AIServiceSettings
    {
        public AzureOpenAISettings? AzureOpenAI { get; set; }
        public OpenAISettings? OpenAI { get; set; }
        public OllamaSettings? Ollama { get; set; }

        /// <summary>
        /// HttpClient timeout in seconds for AI service API calls.
        /// Default is 300 seconds (5 minutes) to handle large payloads in structured output transformations.
        /// </summary>
        public int HttpTimeoutSeconds { get; set; } = 300;
    }
}
