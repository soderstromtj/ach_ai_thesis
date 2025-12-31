using System;
using System.Collections.Generic;

namespace NIU.ACH_AI.Infrastructure.Persistence.Models;

/// <summary>
/// Represents a piece of evidence extracted during the analysis.
/// Maps to the EVIDENCE table.
/// </summary>
public partial class Evidence
{
    /// <summary>
    /// Gets or sets the unique identifier for the evidence.
    /// </summary>
    public Guid EvidenceId { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the step execution that produced this evidence.
    /// </summary>
    public Guid StepExecutionId { get; set; }

    /// <summary>
    /// Gets or sets the claim or statement extracted as evidence.
    /// </summary>
    public string Claim { get; set; } = null!;

    /// <summary>
    /// Gets or sets the snippet of text from the source that supports the claim.
    /// </summary>
    public string? ReferenceSnippet { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the evidence type.
    /// </summary>
    public int EvidenceTypeId { get; set; }

    /// <summary>
    /// Gets or sets additional notes or observations about the evidence.
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when the evidence was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the evaluations associated with this evidence.
    /// </summary>
    public virtual ICollection<EvidenceHypothesisEvaluation> EvidenceHypothesisEvaluations { get; set; } = new List<EvidenceHypothesisEvaluation>();

    /// <summary>
    /// Gets or sets the type of this evidence.
    /// </summary>
    public virtual EvidenceType EvidenceType { get; set; } = null!;

    /// <summary>
    /// Gets or sets the step execution associated with this evidence.
    /// </summary>
    public virtual StepExecution StepExecution { get; set; } = null!;
}
