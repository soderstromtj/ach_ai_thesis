using System;
using System.Collections.Generic;

namespace NIU.ACH_AI.Infrastructure.Persistence.Models;

/// <summary>
/// Represents a scenario or context for analysis experiments.
/// Maps to the SCENARIOS table.
/// </summary>
public partial class Scenario
{
    /// <summary>
    /// Gets or sets the unique identifier for the scenario.
    /// </summary>
    public Guid ScenarioId { get; set; }

    /// <summary>
    /// Gets or sets the descriptive context or background of the scenario.
    /// </summary>
    public string Context { get; set; } = null!;

    /// <summary>
    /// Gets or sets the collection of experiments associated with this scenario.
    /// </summary>
    public virtual ICollection<Experiment> Experiments { get; set; } = new List<Experiment>();
}
