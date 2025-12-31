using System;
using System.Collections.Generic;

namespace NIU.ACH_AI.Infrastructure.Persistence.Models;

/// <summary>
/// Represents a step in the Analysis of Competing Hypotheses (ACH) process.
/// Maps to the ACH_STEPS table.
/// </summary>
public partial class AchStep
{
    /// <summary>
    /// Gets or sets the unique identifier for the ACH step.
    /// </summary>
    public int AchStepId { get; set; }

    /// <summary>
    /// Gets or sets the unique name of the step.
    /// </summary>
    public string StepName { get; set; } = null!;

    /// <summary>
    /// Gets or sets the order execution of the step.
    /// </summary>
    public int StepOrder { get; set; }

    /// <summary>
    /// Gets or sets the description of the step.
    /// </summary>
    public string Description { get; set; } = null!;

    /// <summary>
    /// Gets or sets a value indicating if this is a primary ACH step.
    /// </summary>
    public int PrimaryAchStep { get; set; }

    /// <summary>
    /// Gets or sets the collection of executions associated with this step.
    /// </summary>
    public virtual ICollection<StepExecution> StepExecutions { get; set; } = new List<StepExecution>();
}
