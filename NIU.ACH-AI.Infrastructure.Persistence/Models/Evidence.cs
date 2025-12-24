using System;
using System.Collections.Generic;

namespace NIU.ACH_AI.Infrastructure.Persistence.Models;

public partial class Evidence
{
    public Guid EvidenceId { get; set; }

    public Guid StepExecutionId { get; set; }

    public string Claim { get; set; } = null!;

    public string? ReferenceSnippet { get; set; }

    public int EvidenceTypeId { get; set; }

    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual ICollection<EvidenceHypothesisEvaluation> EvidenceHypothesisEvaluations { get; set; } = new List<EvidenceHypothesisEvaluation>();

    public virtual EvidenceType EvidenceType { get; set; } = null!;

    public virtual StepExecution StepExecution { get; set; } = null!;
}
