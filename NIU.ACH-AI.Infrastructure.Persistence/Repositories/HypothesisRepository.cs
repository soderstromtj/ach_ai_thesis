using Microsoft.EntityFrameworkCore;
using NIU.ACH_AI.Domain.Entities;
using NIU.ACH_AI.Infrastructure.Persistence.Mappers;
using NIU.ACH_AI.Infrastructure.Persistence.Models;
using Hypothesis = NIU.ACH_AI.Domain.Entities.Hypothesis;

namespace NIU.ACH_AI.Infrastructure.Persistence.Repositories;

/// <summary>
/// Implementation of hypothesis repository using Entity Framework Core.
/// Handles mapping between domain and database entities internally.
/// </summary>
public class HypothesisRepository : IHypothesisRepository
{
    private readonly AchAIDbContext _context;

    /// <summary>
    /// Initializes a new instance of the <see cref="HypothesisRepository"/> class.
    /// </summary>
    /// <param name="context">The database context.</param>
    public HypothesisRepository(AchAIDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    /// <summary>
    /// Saves a batch of hypothesis entities to the database.
    /// </summary>
    /// <param name="hypotheses">The list of hypothesis domain entities to save.</param>
    /// <param name="stepExecutionId">The ID of the step execution associated with the hypotheses.</param>
    /// <param name="isRefined">Indicates whether these are refined hypotheses.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns>A list of saved hypothesis domain entities with populated IDs.</returns>
    public async Task<List<Hypothesis>> SaveBatchAsync(
        IEnumerable<Hypothesis> hypotheses,
        Guid stepExecutionId,
        bool isRefined = false,
        CancellationToken cancellationToken = default)
    {
        if (hypotheses == null || !hypotheses.Any())
            return new List<Hypothesis>();

        // Map domain entities to database entities
        var dbEntities = hypotheses
            .Select(h => HypothesisMapper.ToDatabase(h, stepExecutionId, isRefined))
            .ToList();

        // Save to database
        await _context.Hypotheses.AddRangeAsync(dbEntities, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        // Map back to domain entities to return with IDs
        return HypothesisMapper.ToDomain(dbEntities);
    }

    /// <summary>
    /// Retrieves all hypotheses associated with a specific step execution.
    /// </summary>
    /// <param name="stepExecutionId">The ID of the step execution.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns>A list of hypothesis domain entities.</returns>
    public async Task<List<Hypothesis>> GetByStepExecutionIdAsync(
        Guid stepExecutionId,
        CancellationToken cancellationToken = default)
    {
        var dbEntities = await _context.Hypotheses
            .Where(h => h.StepExecutionId == stepExecutionId)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return HypothesisMapper.ToDomain(dbEntities);
    }

    /// <summary>
    /// Retrieves a specific hypothesis entity by its unique identifier.
    /// </summary>
    /// <param name="hypothesisId">The unique identifier of the hypothesis.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns>The hypothesis domain entity if found; otherwise, null.</returns>
    public async Task<Hypothesis?> GetByIdAsync(
        Guid hypothesisId,
        CancellationToken cancellationToken = default)
    {
        var dbEntity = await _context.Hypotheses
            .AsNoTracking()
            .FirstOrDefaultAsync(h => h.HypothesisId == hypothesisId, cancellationToken);

        return dbEntity != null ? HypothesisMapper.ToDomain(dbEntity) : null;
    }

    /// <summary>
    /// Retrieves refined hypotheses associated with a specific step execution.
    /// </summary>
    /// <param name="stepExecutionId">The ID of the step execution.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns>A list of refined hypothesis domain entities.</returns>
    public async Task<List<Hypothesis>> GetRefinedByStepExecutionIdAsync(
        Guid stepExecutionId,
        CancellationToken cancellationToken = default)
    {
        var dbEntities = await _context.Hypotheses
            .Where(h => h.StepExecutionId == stepExecutionId && h.IsRefined)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return HypothesisMapper.ToDomain(dbEntities);
    }

    /// <summary>
    /// Marks a specific hypothesis as refined.
    /// </summary>
    /// <param name="hypothesisId">The unique identifier of the hypothesis to update.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    public async Task MarkAsRefinedAsync(
        Guid hypothesisId,
        CancellationToken cancellationToken = default)
    {
        var dbEntity = await _context.Hypotheses
            .FirstOrDefaultAsync(h => h.HypothesisId == hypothesisId, cancellationToken);

        if (dbEntity != null)
        {
            dbEntity.IsRefined = true;
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
