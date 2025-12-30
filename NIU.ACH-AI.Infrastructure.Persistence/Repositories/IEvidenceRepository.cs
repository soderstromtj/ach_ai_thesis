using NIU.ACH_AI.Domain.Entities;

namespace NIU.ACH_AI.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repository for evidence persistence operations.
/// Works exclusively with domain entities - no database models exposed.
/// </summary>
public interface IEvidenceRepository
{
    /// <summary>
    /// Saves a batch of evidence items extracted by AI for a given step execution.
    /// </summary>
    Task<List<Evidence>> SaveBatchAsync(
        IEnumerable<Evidence> evidenceList,
        Guid stepExecutionId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all evidence for a specific step execution.
    /// </summary>
    Task<List<Evidence>> GetByStepExecutionIdAsync(
        Guid stepExecutionId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a single evidence item by its ID.
    /// </summary>
    Task<Evidence?> GetByIdAsync(
        Guid evidenceId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves evidence filtered by type for a step execution.
    /// </summary>
    Task<List<Evidence>> GetByTypeAsync(
        Guid stepExecutionId,
        NIU.ACH_AI.Domain.ValueObjects.EvidenceType type,
        CancellationToken cancellationToken = default);
}
