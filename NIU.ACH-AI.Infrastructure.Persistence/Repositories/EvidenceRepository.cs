using Microsoft.EntityFrameworkCore;
using NIU.ACH_AI.Domain.Entities;
using NIU.ACH_AI.Infrastructure.Persistence.Mappers;
using NIU.ACH_AI.Infrastructure.Persistence.Models;
using Evidence = NIU.ACH_AI.Domain.Entities.Evidence;

namespace NIU.ACH_AI.Infrastructure.Persistence.Repositories;

/// <summary>
/// Implementation of evidence repository using Entity Framework Core.
/// Handles mapping between domain and database entities internally.
/// </summary>
public class EvidenceRepository : IEvidenceRepository
{
    private readonly AchAIDbContext _context;

    /// <summary>
    /// Initializes a new instance of the <see cref="EvidenceRepository"/> class.
    /// </summary>
    /// <param name="context">The database context.</param>
    public EvidenceRepository(AchAIDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    /// <summary>
    /// Saves a batch of evidence entities to the database.
    /// </summary>
    /// <param name="evidenceList">The list of evidence domain entities to save.</param>
    /// <param name="stepExecutionId">The ID of the step execution associated with the evidence.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns>A list of saved evidence domain entities with populated IDs.</returns>
    public async Task<List<Evidence>> SaveBatchAsync(
        IEnumerable<Evidence> evidenceList,
        Guid stepExecutionId,
        CancellationToken cancellationToken = default)
    {
        if (evidenceList == null || !evidenceList.Any())
            return new List<Evidence>();

        // Map domain entities to database entities
        var dbEntities = evidenceList
            .Select(e => EvidenceMapper.ToDatabase(e, stepExecutionId))
            .ToList();

        // Save to database
        await _context.Evidences.AddRangeAsync(dbEntities, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        // Map back to domain entities to return with IDs
        return EvidenceMapper.ToDomain(dbEntities);
    }

    /// <summary>
    /// Retrieves all evidence associated with a specific step execution.
    /// </summary>
    /// <param name="stepExecutionId">The ID of the step execution.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns>A list of evidence domain entities.</returns>
    public async Task<List<Evidence>> GetByStepExecutionIdAsync(
        Guid stepExecutionId,
        CancellationToken cancellationToken = default)
    {
        var dbEntities = await _context.Evidences
            .Where(e => e.StepExecutionId == stepExecutionId)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return EvidenceMapper.ToDomain(dbEntities);
    }

    /// <summary>
    /// Retrieves a specific evidence entity by its unique identifier.
    /// </summary>
    /// <param name="evidenceId">The unique identifier of the evidence.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns>The evidence domain entity if found; otherwise, null.</returns>
    public async Task<Evidence?> GetByIdAsync(
        Guid evidenceId,
        CancellationToken cancellationToken = default)
    {
        var dbEntity = await _context.Evidences
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.EvidenceId == evidenceId, cancellationToken);

        return dbEntity != null ? EvidenceMapper.ToDomain(dbEntity) : null;
    }

    /// <summary>
    /// Retrieves evidence of a specific type for a given step execution.
    /// </summary>
    /// <param name="stepExecutionId">The ID of the step execution.</param>
    /// <param name="type">The type of evidence to retrieve.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns>A list of evidence domain entities matching the specified type.</returns>
    public async Task<List<Evidence>> GetByTypeAsync(
        Guid stepExecutionId,
        NIU.ACH_AI.Domain.ValueObjects.EvidenceType type,
        CancellationToken cancellationToken = default)
    {
        int typeId = (int)type;

        var dbEntities = await _context.Evidences
            .Where(e => e.StepExecutionId == stepExecutionId && e.EvidenceTypeId == typeId)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return EvidenceMapper.ToDomain(dbEntities);
    }
}
