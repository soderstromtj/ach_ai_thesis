namespace SemanticKernelPractice.Configuration
{
    public class AzureOpenAISettings
    {
        public string ApiKey { get; set; } = string.Empty;
        public string Endpoint { get; set; } = string.Empty;
        public string DeploymentName { get; set; } = string.Empty;
        public string? ModelId { get; set; } = null;
        public string? ServiceId { get; set; } = null;
    }
}