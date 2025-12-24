using System;
using System.Collections.Generic;

namespace NIU.ACH_AI.Infrastructure.Persistence.Models;

public partial class EvaluationScore
{
    public int EvaluationScoreId { get; set; }

    public string ScoreName { get; set; } = null!;

    public int ScoreValue { get; set; }

    public string? Description { get; set; }

    public virtual ICollection<EvidenceHypothesisEvaluation> EvidenceHypothesisEvaluations { get; set; } = new List<EvidenceHypothesisEvaluation>();
}
