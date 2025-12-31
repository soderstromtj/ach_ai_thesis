using System;
using System.Collections.Generic;

namespace NIU.ACH_AI.Infrastructure.Persistence.Models;

/// <summary>
/// Represents the configuration for an AI agent within a specific step execution.
/// Maps to the AGENT_CONFIGURATIONS table.
/// </summary>
public partial class AgentConfiguration
{
    /// <summary>
    /// Gets or sets the unique identifier for the agent configuration.
    /// </summary>
    public Guid AgentConfigurationId { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the associated step execution.
    /// </summary>
    public Guid StepExecutionId { get; set; }

    /// <summary>
    /// Gets or sets the name of the agent.
    /// </summary>
    public string AgentName { get; set; } = null!;

    /// <summary>
    /// Gets or sets the description of the agent's role.
    /// </summary>
    public string Description { get; set; } = null!;

    /// <summary>
    /// Gets or sets the instructions provided to the agent.
    /// </summary>
    public string Instructions { get; set; } = null!;

    /// <summary>
    /// Gets or sets the identifier of the AI provider used by the agent.
    /// </summary>
    public Guid ProviderId { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the AI model used by the agent.
    /// </summary>
    public Guid ModelId { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when the configuration was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when the configuration was last updated.
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// Gets or sets the collection of agent responses associated with this configuration.
    /// </summary>
    public virtual ICollection<AgentResponse> AgentResponses { get; set; } = new List<AgentResponse>();

    /// <summary>
    /// Gets or sets the model associated with this configuration.
    /// </summary>
    public virtual Model Model { get; set; } = null!;

    /// <summary>
    /// Gets or sets the provider associated with this configuration.
    /// </summary>
    public virtual Provider Provider { get; set; } = null!;

    /// <summary>
    /// Gets or sets the step execution associated with this configuration.
    /// </summary>
    public virtual StepExecution StepExecution { get; set; } = null!;
}
