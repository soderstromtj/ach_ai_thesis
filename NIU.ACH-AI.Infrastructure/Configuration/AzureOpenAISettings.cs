namespace NIU.ACH_AI.Infrastructure.Configuration
{
    /// <summary>
    /// Defines configurations specific to the Azure OpenAI service.
    /// </summary>
    public class AzureOpenAISettings
    {
        /// <summary>
        /// Stores the authentication key.
        /// </summary>
        public string ApiKey { get; set; } = string.Empty;

        /// <summary>
        /// Stores the URL for the Azure resource.
        /// </summary>
        public string Endpoint { get; set; } = string.Empty;

        /// <summary>
        /// Identifies the specific model deployment to use.
        /// </summary>
        public string DeploymentName { get; set; } = string.Empty;

        /// <summary>
        /// Optionally overrides the default model identifier.
        /// </summary>
        public string? ModelId { get; set; } = null;

        /// <summary>
        /// Optionally identifies the service instance.
        /// </summary>
        public string? ServiceId { get; set; } = null;
    }
}