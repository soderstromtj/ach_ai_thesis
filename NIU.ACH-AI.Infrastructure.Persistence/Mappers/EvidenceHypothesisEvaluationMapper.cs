using DomainEntity = NIU.ACH_AI.Domain.Entities;
using DbModel = NIU.ACH_AI.Infrastructure.Persistence.Models;

namespace NIU.ACH_AI.Infrastructure.Persistence.Mappers;

/// <summary>
/// Maps between Domain EvidenceHypothesisEvaluation entities and Database models.
/// </summary>
public static class EvidenceHypothesisEvaluationMapper
{
    /// <summary>
    /// Converts a domain evaluation (from AI) to a database entity for persistence.
    /// Requires explicit IDs for the related hypothesis and evidence that are already persisted.
    /// </summary>
    public static DbModel.EvidenceHypothesisEvaluation ToDatabase(
        DomainEntity.EvidenceHypothesisEvaluation domain,
        Guid stepExecutionId,
        Guid hypothesisId,
        Guid evidenceId)
    {
        return new DbModel.EvidenceHypothesisEvaluation
        {
            EvidenceHypothesisEvaluationId = Guid.NewGuid(),
            StepExecutionId = stepExecutionId,
            HypothesisId = hypothesisId,
            EvidenceId = evidenceId,
            EvaluationScoreId = (int)domain.Score, // Enum to int
            Rationale = domain.ScoreRationale,
            ConfidenceScore = domain.ConfidenceLevel,
            ConfidenceRationale = domain.ConfidenceRationale,
            CreatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Converts a database evaluation entity back to a domain entity.
    /// Includes nested Hypothesis and Evidence entities from navigation properties.
    /// </summary>
    public static DomainEntity.EvidenceHypothesisEvaluation ToDomain(
        DbModel.EvidenceHypothesisEvaluation database)
    {
        return new DomainEntity.EvidenceHypothesisEvaluation
        {
            Hypothesis = HypothesisMapper.ToDomain(database.Hypothesis),
            Evidence = EvidenceMapper.ToDomain(database.Evidence),
            Score = (NIU.ACH_AI.Domain.ValueObjects.EvaluationScore)database.EvaluationScoreId,
            ScoreRationale = database.Rationale ?? string.Empty,
            ConfidenceLevel = database.ConfidenceScore ?? 0,
            ConfidenceRationale = database.ConfidenceRationale ?? string.Empty
        };
    }

    /// <summary>
    /// Converts multiple database entities to domain entities.
    /// </summary>
    public static List<DomainEntity.EvidenceHypothesisEvaluation> ToDomain(
        IEnumerable<DbModel.EvidenceHypothesisEvaluation> databaseEntities)
    {
        return databaseEntities.Select(ToDomain).ToList();
    }
}
