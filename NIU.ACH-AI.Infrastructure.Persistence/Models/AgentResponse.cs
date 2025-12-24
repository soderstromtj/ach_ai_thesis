using System;
using System.Collections.Generic;

namespace NIU.ACH_AI.Infrastructure.Persistence.Models;

public partial class AgentResponse
{
    public Guid AgentResponseId { get; set; }

    public Guid StepExecutionId { get; set; }

    public Guid AgentConfigurationId { get; set; }

    public string AgentName { get; set; } = null!;

    public int? InputTokenCount { get; set; }

    public int? OutputTokenCount { get; set; }

    public int? ContentLength { get; set; }

    public string? Content { get; set; }

    public int? TurnNumber { get; set; }

    public long? ResponseDuration { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual AgentConfiguration AgentConfiguration { get; set; } = null!;

    public virtual StepExecution StepExecution { get; set; } = null!;
}
