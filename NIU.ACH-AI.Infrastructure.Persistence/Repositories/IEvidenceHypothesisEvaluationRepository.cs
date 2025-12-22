using NIU.ACH_AI.Domain.Entities;

namespace NIU.ACH_AI.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repository for evidence-hypothesis evaluation persistence operations.
/// Works exclusively with domain entities - no database models exposed.
/// </summary>
public interface IEvidenceHypothesisEvaluationRepository
{
    /// <summary>
    /// Saves a batch of evaluations for a given step execution.
    /// Requires that the referenced hypotheses and evidence are already persisted.
    /// </summary>
    Task SaveBatchAsync(
        IEnumerable<EvidenceHypothesisEvaluation> evaluations,
        Guid stepExecutionId,
        Dictionary<string, Guid> hypothesisIdMap, // Maps ShortTitle -> HypothesisId
        Dictionary<string, Guid> evidenceIdMap,   // Maps Claim -> EvidenceId
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all evaluations for a specific step execution.
    /// Includes nested Hypothesis and Evidence entities.
    /// </summary>
    Task<List<EvidenceHypothesisEvaluation>> GetByStepExecutionIdAsync(
        Guid stepExecutionId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves evaluations for a specific hypothesis.
    /// </summary>
    Task<List<EvidenceHypothesisEvaluation>> GetByHypothesisIdAsync(
        Guid hypothesisId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves evaluations for a specific piece of evidence.
    /// </summary>
    Task<List<EvidenceHypothesisEvaluation>> GetByEvidenceIdAsync(
        Guid evidenceId,
        CancellationToken cancellationToken = default);
}
