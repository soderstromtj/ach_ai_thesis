using System;
using System.Collections.Generic;

namespace NIU.ACH_AI.Infrastructure.Persistence.Models;

/// <summary>
/// Represents the execution of a specific ACH step within an experiment.
/// Maps to the STEP_EXECUTIONS table.
/// </summary>
public partial class StepExecution
{
    /// <summary>
    /// Gets or sets the unique identifier for the step execution.
    /// </summary>
    public Guid StepExecutionId { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the experiment this execution belongs to.
    /// </summary>
    public Guid ExperimentId { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the ACH step definition.
    /// </summary>
    public int AchStepId { get; set; }

    /// <summary>
    /// Gets or sets the name of the ACH step.
    /// </summary>
    public string AchStepName { get; set; } = null!;

    /// <summary>
    /// Gets or sets the description of the execution instance.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the specific instructions used for this execution task.
    /// </summary>
    public string? TaskInstructions { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the orchestration type used.
    /// </summary>
    public Guid? OrchestrationTypeId { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when the execution started.
    /// </summary>
    public DateTime? DatetimeStart { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when the execution ended.
    /// </summary>
    public DateTime? DatetimeEnd { get; set; }

    /// <summary>
    /// Gets or sets the current status of the execution (e.g., NotStarted, InProgress, Completed, Failed).
    /// </summary>
    public string? ExecutionStatus { get; set; }

    /// <summary>
    /// Gets or sets the error message if the execution failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets the type of error if the execution failed.
    /// </summary>
    public string? ErrorType { get; set; }

    /// <summary>
    /// Gets or sets the number of retries attempted for this step.
    /// </summary>
    public int? RetryCount { get; set; }

    /// <summary>
    /// Gets or sets the ACH step definition associated with this execution.
    /// </summary>
    public virtual AchStep AchStep { get; set; } = null!;

    /// <summary>
    /// Gets or sets the collection of agent configurations used in this execution.
    /// </summary>
    public virtual ICollection<AgentConfiguration> AgentConfigurations { get; set; } = new List<AgentConfiguration>();

    /// <summary>
    /// Gets or sets the collection of agent responses generated in this execution.
    /// </summary>
    public virtual ICollection<AgentResponse> AgentResponses { get; set; } = new List<AgentResponse>();

    /// <summary>
    /// Gets or sets the collection of evidence-hypothesis evaluations produced in this execution.
    /// </summary>
    public virtual ICollection<EvidenceHypothesisEvaluation> EvidenceHypothesisEvaluations { get; set; } = new List<EvidenceHypothesisEvaluation>();

    /// <summary>
    /// Gets or sets the collection of evidence items extracted in this execution.
    /// </summary>
    public virtual ICollection<Evidence> Evidences { get; set; } = new List<Evidence>();

    /// <summary>
    /// Gets or sets the experiment associated with this execution.
    /// </summary>
    public virtual Experiment Experiment { get; set; } = null!;

    /// <summary>
    /// Gets or sets the collection of hypotheses generated or processed in this execution.
    /// </summary>
    public virtual ICollection<Hypothesis> Hypotheses { get; set; } = new List<Hypothesis>();

    /// <summary>
    /// Gets or sets the orchestration type used for this execution.
    /// </summary>
    public virtual OrchestrationType? OrchestrationType { get; set; }
}
