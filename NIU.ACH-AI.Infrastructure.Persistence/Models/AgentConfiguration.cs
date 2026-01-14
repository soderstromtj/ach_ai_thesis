using System;
using System.Collections.Generic;

namespace NIU.ACH_AI.Infrastructure.Persistence.Models;

public partial class AgentConfiguration
{
    public Guid AgentConfigurationId { get; set; }

    public Guid StepExecutionId { get; set; }

    public string AgentName { get; set; } = null!;

    public string Description { get; set; } = null!;

    public string Instructions { get; set; } = null!;

    public Guid ProviderId { get; set; }

    public Guid ModelId { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual ICollection<AgentResponse> AgentResponses { get; set; } = new List<AgentResponse>();

    public virtual Model Model { get; set; } = null!;

    public virtual Provider Provider { get; set; } = null!;

    public virtual StepExecution StepExecution { get; set; } = null!;
}
