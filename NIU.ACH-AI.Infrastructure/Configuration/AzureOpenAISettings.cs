namespace NIU.ACH_AI.Infrastructure.Configuration
{
    /// <summary>
    /// Configuration settings for Azure OpenAI service.
    /// </summary>
    public class AzureOpenAISettings
    {
        /// <summary>
        /// Gets or sets the API key for authentication.
        /// </summary>
        public string ApiKey { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the endpoint URL for the Azure OpenAI resource.
        /// </summary>
        public string Endpoint { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the deployment name for the model.
        /// </summary>
        public string DeploymentName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the optional model ID override.
        /// </summary>
        public string? ModelId { get; set; } = null;

        /// <summary>
        /// Gets or sets the optional service ID.
        /// </summary>
        public string? ServiceId { get; set; } = null;
    }
}