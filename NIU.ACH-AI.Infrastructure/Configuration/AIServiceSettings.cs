namespace NIU.ACH_AI.Infrastructure.Configuration
{
    /// <summary>
    /// Defines global settings for different AI service providers.
    /// Provider selection happens per-experiment in ExperimentAIServiceSettings.
    /// </summary>
    public class AIServiceSettings
    {
        /// <summary>
        /// Specifies the configuration for Azure OpenAI.
        /// </summary>
        public AzureOpenAISettings? AzureOpenAI { get; set; }

        /// <summary>
        /// Specifies the configuration for general OpenAI.
        /// </summary>
        public OpenAISettings? OpenAI { get; set; }

        /// <summary>
        /// Specifies the configuration for local Ollama instances.
        /// </summary>
        public OllamaSettings? Ollama { get; set; }

        /// <summary>
        /// Sets the maximum wait time for API calls. 
        /// Defaults to 300 seconds (5 minutes) to accommodate large data transfers.
        /// </summary>
        public int HttpTimeoutSeconds { get; set; } = 300;
    }
}
