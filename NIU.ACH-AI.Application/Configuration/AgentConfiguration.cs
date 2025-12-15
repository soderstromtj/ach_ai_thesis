namespace NIU.ACH_AI.Application.Configuration
{
    public class AgentConfiguration
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Instructions { get; set; } = string.Empty;

        /// <summary>
        /// Optional service ID to specify which AI provider this agent should use.
        /// Valid values: "ollama", "openai", "azure"
        /// If not specified, the kernel's default service will be used.
        /// </summary>
        public string? ServiceId { get; set; }

        /// <summary>
        /// Optional model ID to specify which specific model this agent should use.
        /// If specified, overrides the default ModelId from the provider settings.
        /// Examples: "gpt-4o", "gpt-3.5-turbo", "o1-preview", "claude-3-opus", etc.
        /// If not specified, uses the ModelId from the provider settings.
        /// </summary>
        public string? ModelId { get; set; }
    }
}
