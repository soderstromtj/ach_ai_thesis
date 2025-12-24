using DomainEntity = NIU.ACH_AI.Domain.Entities;
using DbModel = NIU.ACH_AI.Infrastructure.Persistence.Models;

namespace NIU.ACH_AI.Infrastructure.Persistence.Mappers;

/// <summary>
/// Maps between Domain Evidence entities and Database Evidence models.
/// </summary>
public static class EvidenceMapper
{
    /// <summary>
    /// Converts a domain evidence (from AI) to a database entity for persistence.
    /// Note: Handles the EvidenceType enum to EvidenceTypeId int conversion.
    /// </summary>
    public static DbModel.Evidence ToDatabase(
        DomainEntity.Evidence domain,
        Guid stepExecutionId)
    {
        // First check if domain.EvidenceId is not empty or Guid.Empty, if it is not empty, we should try to parse it into a Guid
        Guid evidenceId = Guid.Empty.Equals(domain.EvidenceId) || string.IsNullOrEmpty(domain.EvidenceId.ToString()) 
            ? Guid.NewGuid() 
            : Guid.TryParse(domain.EvidenceId.ToString(), out var parsedGuid) ? parsedGuid : Guid.NewGuid();

        return new DbModel.Evidence
        {
            EvidenceId = evidenceId, 
            StepExecutionId = stepExecutionId,
            Claim = domain.Claim,
            ReferenceSnippet = domain.ReferenceSnippet,
            EvidenceTypeId = (int)domain.Type, // Enum to int
            Notes = domain.Notes ?? string.Empty,
            CreatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Converts a database evidence entity back to a domain entity.
    /// </summary>
    public static DomainEntity.Evidence ToDomain(DbModel.Evidence database)
    {
        return new DomainEntity.Evidence
        {
            EvidenceId = database.EvidenceId,
            Claim = database.Claim,
            ReferenceSnippet = database.ReferenceSnippet,
            Type = (NIU.ACH_AI.Domain.ValueObjects.EvidenceType)database.EvidenceTypeId, // Int to enum
            Notes = database.Notes ?? string.Empty
        };
    }

    /// <summary>
    /// Converts multiple database entities to domain entities.
    /// </summary>
    public static List<DomainEntity.Evidence> ToDomain(IEnumerable<DbModel.Evidence> databaseEntities)
    {
        return databaseEntities.Select(ToDomain).ToList();
    }
}
