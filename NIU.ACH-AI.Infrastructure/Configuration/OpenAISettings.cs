namespace NIU.ACH_AI.Infrastructure.Configuration
{
    public class OpenAISettings
    {
        public string ApiKey { get; set; } = string.Empty;
        public string ModelId { get; set; } = "gpt-4o";
        public string? OrganizationId { get; set; }
        public string? ServiceId { get; set; }
    }
}
