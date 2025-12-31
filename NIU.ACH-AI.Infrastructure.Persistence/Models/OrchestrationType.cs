using System;
using System.Collections.Generic;

namespace NIU.ACH_AI.Infrastructure.Persistence.Models;

/// <summary>
/// Represents a type of workflow orchestration pattern (e.g., Sequential, Concurrent).
/// Maps to the ORCHESTRATION_TYPES table.
/// </summary>
public partial class OrchestrationType
{
    /// <summary>
    /// Gets or sets the unique identifier for the orchestration type.
    /// </summary>
    public Guid OrchestrationTypeId { get; set; }

    /// <summary>
    /// Gets or sets the description of the orchestration pattern.
    /// </summary>
    public string Description { get; set; } = null!;

    /// <summary>
    /// Gets or sets the collection of step executions using this orchestration type.
    /// </summary>
    public virtual ICollection<StepExecution> StepExecutions { get; set; } = new List<StepExecution>();
}
