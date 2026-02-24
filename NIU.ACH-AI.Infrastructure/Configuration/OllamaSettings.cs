namespace NIU.ACH_AI.Infrastructure.Configuration
{
    /// <summary>
    /// Defines configurations specific to the local Ollama AI service.
    /// </summary>
    public class OllamaSettings
    {
        /// <summary>
        /// Stores the connection URL for the local service.
        /// Defaults to http://localhost:11434.
        /// </summary>
        public string Endpoint { get; set; } = "http://localhost:11434";

        /// <summary>
        /// Identifies which model to run by default.
        /// </summary>
        public string ModelId { get; set; } = "llama2";

        /// <summary>
        /// Optionally identifies the service instance.
        /// </summary>
        public string? ServiceId { get; set; } = null;
    }
}
