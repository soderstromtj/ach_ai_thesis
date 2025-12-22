using System;
using System.Collections.Generic;

namespace NIU.ACH_AI.Infrastructure.Persistence.Models;

public partial class Experiment
{
    public Guid ExperimentId { get; set; }

    public string ExperimentName { get; set; } = null!;

    public string? Description { get; set; }

    public string Kiq { get; set; } = null!;

    public Guid ScenarioId { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Scenario Scenario { get; set; } = null!;

    public virtual ICollection<StepExecution> StepExecutions { get; set; } = new List<StepExecution>();
}
