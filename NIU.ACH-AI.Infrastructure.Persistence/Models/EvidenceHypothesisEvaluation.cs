using System;
using System.Collections.Generic;

namespace NIU.ACH_AI.Infrastructure.Persistence.Models;

/// <summary>
/// Represents evaluated relationship between a piece of evidence and a hypothesis.
/// Maps to the EVIDENCE_HYPOTHESIS_EVALUATIONS table.
/// </summary>
public partial class EvidenceHypothesisEvaluation
{
    /// <summary>
    /// Gets or sets the unique identifier for the evaluation.
    /// </summary>
    public Guid EvidenceHypothesisEvaluationId { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the step execution.
    /// </summary>
    public Guid StepExecutionId { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the hypothesis being evaluated.
    /// </summary>
    public Guid HypothesisId { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the evidence used for evaluation.
    /// </summary>
    public Guid EvidenceId { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the evaluation score (e.g., C, I).
    /// </summary>
    public int EvaluationScoreId { get; set; }

    /// <summary>
    /// Gets or sets the rationale for the assigned score.
    /// </summary>
    public string? Rationale { get; set; }

    /// <summary>
    /// Gets or sets the confidence score for the evaluation (0-1).
    /// </summary>
    public decimal? ConfidenceScore { get; set; }

    /// <summary>
    /// Gets or sets the rationale for the confidence score.
    /// </summary>
    public string? ConfidenceRationale { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when the evaluation was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the score definition associated with this evaluation.
    /// </summary>
    public virtual EvaluationScore EvaluationScore { get; set; } = null!;

    /// <summary>
    /// Gets or sets the evidence associated with this evaluation.
    /// </summary>
    public virtual Evidence Evidence { get; set; } = null!;

    /// <summary>
    /// Gets or sets the hypothesis associated with this evaluation.
    /// </summary>
    public virtual Hypothesis Hypothesis { get; set; } = null!;

    /// <summary>
    /// Gets or sets the step execution associated with this evaluation.
    /// </summary>
    public virtual StepExecution StepExecution { get; set; } = null!;
}
