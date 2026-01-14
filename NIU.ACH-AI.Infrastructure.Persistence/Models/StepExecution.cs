using System;
using System.Collections.Generic;

namespace NIU.ACH_AI.Infrastructure.Persistence.Models;

public partial class StepExecution
{
    public Guid StepExecutionId { get; set; }

    public Guid ExperimentId { get; set; }

    public int AchStepId { get; set; }

    public string AchStepName { get; set; } = null!;

    public string? Description { get; set; }

    public string? TaskInstructions { get; set; }

    public Guid? OrchestrationTypeId { get; set; }

    public DateTime? DatetimeStart { get; set; }

    public DateTime? DatetimeEnd { get; set; }

    public string? ExecutionStatus { get; set; }

    public string? ErrorMessage { get; set; }

    public string? ErrorType { get; set; }

    public int? RetryCount { get; set; }

    public virtual AchStep AchStep { get; set; } = null!;

    public virtual ICollection<AgentConfiguration> AgentConfigurations { get; set; } = new List<AgentConfiguration>();

    public virtual ICollection<AgentResponse> AgentResponses { get; set; } = new List<AgentResponse>();

    public virtual ICollection<EvidenceHypothesisEvaluation> EvidenceHypothesisEvaluations { get; set; } = new List<EvidenceHypothesisEvaluation>();

    public virtual ICollection<Evidence> Evidences { get; set; } = new List<Evidence>();

    public virtual Experiment Experiment { get; set; } = null!;

    public virtual ICollection<Hypothesis> Hypotheses { get; set; } = new List<Hypothesis>();

    public virtual OrchestrationType? OrchestrationType { get; set; }
}
