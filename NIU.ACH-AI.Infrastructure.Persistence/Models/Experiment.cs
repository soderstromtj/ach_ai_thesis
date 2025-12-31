using System;
using System.Collections.Generic;

namespace NIU.ACH_AI.Infrastructure.Persistence.Models;

/// <summary>
/// Represents an analysis experiment or run within a scenario.
/// Maps to the EXPERIMENTS table.
/// </summary>
public partial class Experiment
{
    /// <summary>
    /// Gets or sets the unique identifier for the experiment.
    /// </summary>
    public Guid ExperimentId { get; set; }

    /// <summary>
    /// Gets or sets the name of the experiment.
    /// </summary>
    public string ExperimentName { get; set; } = null!;

    /// <summary>
    /// Gets or sets the description of the experiment.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the Key Intelligence Question (KIQ) for this experiment.
    /// </summary>
    public string Kiq { get; set; } = null!;

    /// <summary>
    /// Gets or sets the identifier of the scenario this experiment belongs to.
    /// </summary>
    public Guid ScenarioId { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when the experiment was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the scenario associated with this experiment.
    /// </summary>
    public virtual Scenario Scenario { get; set; } = null!;

    /// <summary>
    /// Gets or sets the collection of step executions within this experiment.
    /// </summary>
    public virtual ICollection<StepExecution> StepExecutions { get; set; } = new List<StepExecution>();
}
