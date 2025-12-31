namespace NIU.ACH_AI.Application.Configuration
{
    /// <summary>
    /// Defines the configuration for a specific AI agent within an ACH step.
    /// </summary>
    /// <remarks>
    /// This includes identity information (Name, Description), behavior instructions, and model selection.
    /// </remarks>
    public class AgentConfiguration
    {
        /// <summary>
        /// Gets or sets the unique name of the agent.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets a brief description of the agent's role.
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the system instructions or persona definition for the agent.
        /// </summary>
        public string Instructions { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the optional service ID to specify which AI provider this agent should use.
        /// </summary>
        /// <value>
        /// Valid values might include "ollama", "openai", "azure". If <c>null</c>, the kernel's default service is used.
        /// </value>
        public string? ServiceId { get; set; }

        /// <summary>
        /// Gets or sets the optional model ID to specify which specific model this agent should use.
        /// </summary>
        /// <value>
        /// Examples: "gpt-4o", "gpt-3.5-turbo". If <c>null</c>, uses the default ModelId from the provider.
        /// </value>
        public string? ModelId { get; set; }

        /// <summary>
        /// Gets or sets the list of tags or roles associated with this agent.
        /// </summary>
        /// <remarks>
        /// Used for dynamic selection strategies (e.g., "Brainstorming", "Screening").
        /// </remarks>
        public List<string> Tags { get; set; } = new();
    }
}
