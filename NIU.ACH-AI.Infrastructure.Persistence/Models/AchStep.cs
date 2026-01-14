using System;
using System.Collections.Generic;

namespace NIU.ACH_AI.Infrastructure.Persistence.Models;

public partial class AchStep
{
    public int AchStepId { get; set; }

    public string StepName { get; set; } = null!;

    public int StepOrder { get; set; }

    public string Description { get; set; } = null!;

    public int PrimaryAchStep { get; set; }

    public virtual ICollection<StepExecution> StepExecutions { get; set; } = new List<StepExecution>();
}
