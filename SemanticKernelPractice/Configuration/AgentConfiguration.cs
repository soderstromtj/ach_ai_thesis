namespace SemanticKernelPractice.Configuration
{
    public class AgentConfiguration
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Instructions { get; set; } = string.Empty;

        /// <summary>
        /// Optional service ID to specify which AI provider this agent should use.
        /// Valid values: "ollama", "openai", "azure", "huggingface"
        /// If not specified, the kernel's default service will be used.
        /// </summary>
        public string? ServiceId { get; set; }
    }
}
