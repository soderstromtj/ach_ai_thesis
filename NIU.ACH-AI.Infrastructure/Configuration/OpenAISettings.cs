namespace NIU.ACH_AI.Infrastructure.Configuration
{
    /// <summary>
    /// Defines configurations specific to the general OpenAI service.
    /// </summary>
    public class OpenAISettings
    {
        /// <summary>
        /// Stores the authentication key.
        /// </summary>
        public string ApiKey { get; set; } = string.Empty;

        /// <summary>
        /// Identifies which model to prioritize.
        /// </summary>
        public string ModelId { get; set; } = "o3";

        /// <summary>
        /// Optionally links requests to a specific organization.
        /// </summary>
        public string? OrganizationId { get; set; }

        /// <summary>
        /// Optionally identifies the service instance.
        /// </summary>
        public string? ServiceId { get; set; }
    }
}
