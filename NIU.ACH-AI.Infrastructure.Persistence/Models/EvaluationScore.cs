using System;
using System.Collections.Generic;

namespace NIU.ACH_AI.Infrastructure.Persistence.Models;

/// <summary>
/// Represents a predefined score (e.g., C, I, C/I) used for evaluating evidence against hypotheses.
/// Maps to the EVALUATION_SCORES table.
/// </summary>
public partial class EvaluationScore
{
    /// <summary>
    /// Gets or sets the unique identifier for the evaluation score.
    /// </summary>
    public int EvaluationScoreId { get; set; }

    /// <summary>
    /// Gets or sets the display name of the score (e.g., "Consistent").
    /// </summary>
    public string ScoreName { get; set; } = null!;

    /// <summary>
    /// Gets or sets the numeric value associated with the score.
    /// </summary>
    public int ScoreValue { get; set; }

    /// <summary>
    /// Gets or sets the description of what the score represents.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the evaluations that use this score.
    /// </summary>
    public virtual ICollection<EvidenceHypothesisEvaluation> EvidenceHypothesisEvaluations { get; set; } = new List<EvidenceHypothesisEvaluation>();
}
