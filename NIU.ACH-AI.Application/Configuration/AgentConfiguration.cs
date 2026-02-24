namespace NIU.ACH_AI.Application.Configuration
{
    /// <summary>
    /// Specifies the operational parameters and identity for an individual artificial intelligence participant.
    /// </summary>
    /// <remarks>
    /// Encapsulates the behavioral instructions and target model specifications needed to instantiate an autonomous actor within a workflow phase.
    /// </remarks>
    public class AgentConfiguration
    {
        /// <summary>
        /// Gets or sets the designated moniker used to distinguish this actor in multi-agent environments.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the high-level summary of the actor's intended function or expertise.
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the system prompt that dictates the rules of engagement and persona.
        /// </summary>
        public string Instructions { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the explicit platform override, determining which ecosystem handles the requests.
        /// </summary>
        /// <value>
        /// Valid values might include "ollama", "openai", "azure". If <c>null</c>, the kernel's default service is used.
        /// </value>
        public string? ServiceId { get; set; }

        /// <summary>
        /// Gets or sets the specific version or tier of the underlying model, allowing for cost or capability tuning.
        /// </summary>
        /// <value>
        /// Examples: "gpt-4o", "gpt-3.5-turbo". If <c>null</c>, uses the default ModelId from the provider.
        /// </value>
        public string? ModelId { get; set; }

        /// <summary>
        /// Gets or sets the categorical labels applied to enable dynamic discovery or filtering during group discussions.
        /// </summary>
        /// <remarks>
        /// Used for dynamic selection strategies (e.g., "Brainstorming", "Screening").
        /// </remarks>
        public List<string> Tags { get; set; } = new();
    }
}
