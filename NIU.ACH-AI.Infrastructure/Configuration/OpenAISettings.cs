namespace NIU.ACH_AI.Infrastructure.Configuration
{
    /// <summary>
    /// Configuration settings for OpenAI service.
    /// </summary>
    public class OpenAISettings
    {
        /// <summary>
        /// Gets or sets the API key for authentication.
        /// </summary>
        public string ApiKey { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the model ID (e.g., "o3").
        /// </summary>
        public string ModelId { get; set; } = "o3";

        /// <summary>
        /// Gets or sets the optional organization ID.
        /// </summary>
        public string? OrganizationId { get; set; }

        /// <summary>
        /// Gets or sets the optional service ID.
        /// </summary>
        public string? ServiceId { get; set; }
    }
}
