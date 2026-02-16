using System;
using MassTransit;

namespace NIU.ACH_AI.Infrastructure.Persistence.Models
{
    /// <summary>
    /// Represents the state of an ACH Experiment workflow saga.
    /// Persisted to the database to ensure resilience.
    /// </summary>
    public class ExperimentState : SagaStateMachineInstance
    {
        /// <summary>
        /// The Correlation ID (Experiment ID).
        /// </summary>
        public Guid CorrelationId { get; set; }

        /// <summary>
        /// The current state of the saga (e.g., "Brainstorming", "RunningStep").
        /// </summary>
        public string CurrentState { get; set; }

        /// <summary>
        /// The index of the current step being executed (0-based).
        /// </summary>
        public int CurrentStepIndex { get; set; }

        /// <summary>
        /// Serialized <see cref="NIU.ACH_AI.Application.Configuration.ExperimentConfiguration"/>.
        /// Stored as JSON to handle dynamic structure flexibly.
        /// </summary>
        public string SerializedConfiguration { get; set; }

        /// <summary>
        /// Serialized <see cref="NIU.ACH_AI.Application.DTOs.OrchestrationPromptInput"/>.
        /// Accumulates context as the workflow progresses.
        /// </summary>
        public string SerializedInput { get; set; }

        /// <summary>
        /// Serialized <see cref="NIU.ACH_AI.Application.DTOs.ACHWorkflowResult"/>.
        /// Accumulates results as the workflow progresses.
        /// </summary>
        public string SerializedResult { get; set; }

        // Context IDs for linking steps (e.g. Brainstorming output used in Refinement)
        public Guid? HypothesisStepExecutionId { get; set; }
        public Guid? RefinedHypothesisStepExecutionId { get; set; }
        public Guid? EvidenceStepExecutionId { get; set; }

        /// <summary>
        /// Total number of evaluations expected in the current batch.
        /// </summary>
        public int TotalEvaluations { get; set; }

        /// <summary>
        /// Number of evaluations completed in the current batch.
        /// </summary>
        public int CompletedEvaluations { get; set; }

        /// <summary>
        /// Timestamp when the saga was created.
        /// </summary>
        public DateTime Created { get; set; }

        /// <summary>
        /// Timestamp when the saga was last updated.
        /// </summary>
        public DateTime Updated { get; set; }

        /// <summary>
        /// RowVersion for Optimistic Concurrency.
        /// </summary>
        public byte[] RowVersion { get; set; }
    }
}
