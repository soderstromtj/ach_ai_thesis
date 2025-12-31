namespace NIU.ACH_AI.Infrastructure.Configuration
{
    /// <summary>
    /// Global AI service provider configurations.
    /// The Provider selection is now per-experiment in ExperimentAIServiceSettings.
    /// </summary>
    public class AIServiceSettings
    {
        /// <summary>
        /// Gets or sets the Azure OpenAI specific settings.
        /// </summary>
        public AzureOpenAISettings? AzureOpenAI { get; set; }

        /// <summary>
        /// Gets or sets the OpenAI specific settings.
        /// </summary>
        public OpenAISettings? OpenAI { get; set; }

        /// <summary>
        /// Gets or sets the Ollama specific settings.
        /// </summary>
        public OllamaSettings? Ollama { get; set; }

        /// <summary>
        /// HttpClient timeout in seconds for AI service API calls.
        /// Default is 300 seconds (5 minutes) to handle large payloads in structured output transformations.
        /// </summary>
        public int HttpTimeoutSeconds { get; set; } = 300;
    }
}
