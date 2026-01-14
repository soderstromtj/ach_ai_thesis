using System;
using System.Collections.Generic;

namespace NIU.ACH_AI.Infrastructure.Persistence.Models;

public partial class Hypothesis
{
    public Guid HypothesisId { get; set; }

    public Guid StepExecutionId { get; set; }

    public string ShortTitle { get; set; } = null!;

    public string HypothesisText { get; set; } = null!;

    public bool IsRefined { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual ICollection<EvidenceHypothesisEvaluation> EvidenceHypothesisEvaluations { get; set; } = new List<EvidenceHypothesisEvaluation>();

    public virtual StepExecution StepExecution { get; set; } = null!;
}
