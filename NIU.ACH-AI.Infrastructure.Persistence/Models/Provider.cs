using System;
using System.Collections.Generic;

namespace NIU.ACH_AI.Infrastructure.Persistence.Models;

/// <summary>
/// Represents a provider of AI models (e.g., OpenAI, Azure OpenAI, Ollama).
/// Maps to the PROVIDERS table.
/// </summary>
public partial class Provider
{
    /// <summary>
    /// Gets or sets the unique identifier for the provider.
    /// </summary>
    public Guid ProviderId { get; set; }

    /// <summary>
    /// Gets or sets the unique name of the provider.
    /// </summary>
    public string ProviderName { get; set; } = null!;

    /// <summary>
    /// Gets or sets the description of the provider.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the provider is currently active.
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when the provider was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the collection of agent configurations using this provider.
    /// </summary>
    public virtual ICollection<AgentConfiguration> AgentConfigurations { get; set; } = new List<AgentConfiguration>();

    /// <summary>
    /// Gets or sets the collection of models offered by this provider.
    /// </summary>
    public virtual ICollection<Model> Models { get; set; } = new List<Model>();
}
