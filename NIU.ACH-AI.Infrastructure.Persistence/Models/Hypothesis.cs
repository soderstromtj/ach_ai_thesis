using System;
using System.Collections.Generic;

namespace NIU.ACH_AI.Infrastructure.Persistence.Models;

/// <summary>
/// Represents a hypothesis generated or refined during the analysis.
/// Maps to the HYPOTHESES table.
/// </summary>
public partial class Hypothesis
{
    /// <summary>
    /// Gets or sets the unique identifier for the hypothesis.
    /// </summary>
    public Guid HypothesisId { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the step execution that created this hypothesis.
    /// </summary>
    public Guid StepExecutionId { get; set; }

    /// <summary>
    /// Gets or sets the short title or label of the hypothesis.
    /// </summary>
    public string ShortTitle { get; set; } = null!;

    /// <summary>
    /// Gets or sets the full text description of the hypothesis.
    /// </summary>
    public string HypothesisText { get; set; } = null!;

    /// <summary>
    /// Gets or sets a value indicating whether this hypothesis has been refined.
    /// </summary>
    public bool IsRefined { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when the hypothesis was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the evaluations associated with this hypothesis.
    /// </summary>
    public virtual ICollection<EvidenceHypothesisEvaluation> EvidenceHypothesisEvaluations { get; set; } = new List<EvidenceHypothesisEvaluation>();

    /// <summary>
    /// Gets or sets the step execution associated with this hypothesis.
    /// </summary>
    public virtual StepExecution StepExecution { get; set; } = null!;
}
