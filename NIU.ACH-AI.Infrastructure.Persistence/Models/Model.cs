using System;
using System.Collections.Generic;

namespace NIU.ACH_AI.Infrastructure.Persistence.Models;

/// <summary>
/// Represents an AI model provided by a specific service provider.
/// Maps to the MODELS table.
/// </summary>
public partial class Model
{
    /// <summary>
    /// Gets or sets the unique identifier for the model.
    /// </summary>
    public Guid ModelId { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the provider offering this model.
    /// </summary>
    public Guid ProviderId { get; set; }

    /// <summary>
    /// Gets or sets the unique name of the model (e.g., "gpt-4").
    /// </summary>
    public string ModelName { get; set; } = null!;

    /// <summary>
    /// Gets or sets the cost per input token.
    /// </summary>
    public decimal? InputTokenCost { get; set; }

    /// <summary>
    /// Gets or sets the cost per cached input token.
    /// </summary>
    public decimal? CachedInputTokenCost { get; set; }

    /// <summary>
    /// Gets or sets the cost per output token.
    /// </summary>
    public decimal? OutputTokenCost { get; set; }

    /// <summary>
    /// Gets or sets the agent configurations using this model.
    /// </summary>
    public virtual ICollection<AgentConfiguration> AgentConfigurations { get; set; } = new List<AgentConfiguration>();

    /// <summary>
    /// Gets or sets the provider associated with this model.
    /// </summary>
    public virtual Provider Provider { get; set; } = null!;
}
