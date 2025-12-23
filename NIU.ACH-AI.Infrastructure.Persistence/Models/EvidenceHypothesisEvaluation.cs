using System;
using System.Collections.Generic;

namespace NIU.ACH_AI.Infrastructure.Persistence.Models;

public partial class EvidenceHypothesisEvaluation
{
    public Guid EvidenceHypothesisEvaluationId { get; set; }

    public Guid StepExecutionId { get; set; }

    public Guid HypothesisId { get; set; }

    public Guid EvidenceId { get; set; }

    public int EvaluationScoreId { get; set; }

    public string? Rationale { get; set; }

    public decimal? ConfidenceScore { get; set; }

    public string? ConfidenceRationale { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual EvaluationScore EvaluationScore { get; set; } = null!;

    public virtual Evidence Evidence { get; set; } = null!;

    public virtual Hypothesis Hypothesis { get; set; } = null!;

    public virtual StepExecution StepExecution { get; set; } = null!;
}
