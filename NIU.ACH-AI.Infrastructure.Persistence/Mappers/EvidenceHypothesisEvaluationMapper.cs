using DomainEntity = NIU.ACH_AI.Domain.Entities;
using DbModel = NIU.ACH_AI.Infrastructure.Persistence.Models;
using NIU.ACH_AI.Domain.ValueObjects;

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
        // Map Enum to Database ID
        // 1	VeryConsistent	2
        // 2	Consistent	    1
        // 3	Neutral	        0
        // 4	Inconsistent	-1
        // 5	VeryInconsistent -2
        int scoreId = domain.Score switch
        {
            EvaluationScore.VeryConsistent => 1,
            EvaluationScore.Consistent => 2,
            EvaluationScore.Neutral => 3,
            EvaluationScore.Inconsistent => 4,
            EvaluationScore.VeryInconsistent => 5,
            _ => 3 // Default to Neutral if unknown
        };

        return new DbModel.EvidenceHypothesisEvaluation
        {
            EvidenceHypothesisEvaluationId = Guid.NewGuid(),
            StepExecutionId = stepExecutionId,
            HypothesisId = hypothesisId,
            EvidenceId = evidenceId,
            EvaluationScoreId = scoreId,
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
        // Map Database ID back to Enum
        EvaluationScore score = database.EvaluationScoreId switch
        {
            1 => EvaluationScore.VeryConsistent,
            2 => EvaluationScore.Consistent,
            3 => EvaluationScore.Neutral,
            4 => EvaluationScore.Inconsistent,
            5 => EvaluationScore.VeryInconsistent,
            _ => EvaluationScore.Neutral
        };

        return new DomainEntity.EvidenceHypothesisEvaluation
        {
            Hypothesis = HypothesisMapper.ToDomain(database.Hypothesis),
            Evidence = EvidenceMapper.ToDomain(database.Evidence),
            Score = score,
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
