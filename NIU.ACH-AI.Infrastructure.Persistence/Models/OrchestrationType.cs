using System;
using System.Collections.Generic;

namespace NIU.ACH_AI.Infrastructure.Persistence.Models;

public partial class OrchestrationType
{
    public Guid OrchestrationTypeId { get; set; }

    public string Description { get; set; } = null!;

    public virtual ICollection<StepExecution> StepExecutions { get; set; } = new List<StepExecution>();
}
