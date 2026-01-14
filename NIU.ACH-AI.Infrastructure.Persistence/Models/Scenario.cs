using System;
using System.Collections.Generic;

namespace NIU.ACH_AI.Infrastructure.Persistence.Models;

public partial class Scenario
{
    public Guid ScenarioId { get; set; }

    public string Context { get; set; } = null!;

    public virtual ICollection<Experiment> Experiments { get; set; } = new List<Experiment>();
}
