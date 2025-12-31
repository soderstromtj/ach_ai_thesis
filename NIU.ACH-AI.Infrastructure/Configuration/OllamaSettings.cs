namespace NIU.ACH_AI.Infrastructure.Configuration
{
    /// <summary>
    /// Configuration settings for Ollama local AI service.
    /// </summary>
    public class OllamaSettings
    {
        /// <summary>
        /// Gets or sets the endpoint URL for the Ollama service.
        /// Default is http://localhost:11434.
        /// </summary>
        public string Endpoint { get; set; } = "http://localhost:11434";

        /// <summary>
        /// Gets or sets the default model ID to use (e.g., "llama2").
        /// </summary>
        public string ModelId { get; set; } = "llama2";

        /// <summary>
        /// Gets or sets the optional service ID.
        /// </summary>
        public string? ServiceId { get; set; } = null;
    }
}
