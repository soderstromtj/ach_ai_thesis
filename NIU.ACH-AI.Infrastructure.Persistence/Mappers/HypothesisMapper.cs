using DomainEntity = NIU.ACH_AI.Domain.Entities;
using DbModel = NIU.ACH_AI.Infrastructure.Persistence.Models;

namespace NIU.ACH_AI.Infrastructure.Persistence.Mappers;

/// <summary>
/// Maps between Domain Hypothesis entities and Database Hypothesis models.
/// </summary>
public static class HypothesisMapper
{
    /// <summary>
    /// Converts a domain hypothesis (from AI) to a database entity for persistence.
    /// </summary>
    public static DbModel.Hypothesis ToDatabase(
        DomainEntity.Hypothesis domain,
        Guid stepExecutionId)
    {
        return new DbModel.Hypothesis
        {
            HypothesisId = Guid.NewGuid(),
            StepExecutionId = stepExecutionId,
            ShortTitle = domain.ShortTitle,
            HypothesisText = domain.HypothesisText,
            IsRefined = false,
            CreatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Converts a database hypothesis entity back to a domain entity.
    /// </summary>
    public static DomainEntity.Hypothesis ToDomain(DbModel.Hypothesis database)
    {
        return new DomainEntity.Hypothesis
        {
            ShortTitle = database.ShortTitle,
            HypothesisText = database.HypothesisText
        };
    }

    /// <summary>
    /// Converts multiple database entities to domain entities.
    /// </summary>
    public static List<DomainEntity.Hypothesis> ToDomain(IEnumerable<DbModel.Hypothesis> databaseEntities)
    {
        return databaseEntities.Select(ToDomain).ToList();
    }
}
