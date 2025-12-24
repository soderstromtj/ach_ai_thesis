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

    public HypothesisRepository(AchAIDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task SaveBatchAsync(
        IEnumerable<Hypothesis> hypotheses,
        Guid stepExecutionId,
        bool isRefined = false,
        CancellationToken cancellationToken = default)
    {
        if (hypotheses == null || !hypotheses.Any())
            return;

        // Map domain entities to database entities
        var dbEntities = hypotheses
            .Select(h => HypothesisMapper.ToDatabase(h, stepExecutionId, isRefined))
            .ToList();

        // Save to database
        await _context.Hypotheses.AddRangeAsync(dbEntities, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

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

    public async Task<Hypothesis?> GetByIdAsync(
        Guid hypothesisId,
        CancellationToken cancellationToken = default)
    {
        var dbEntity = await _context.Hypotheses
            .AsNoTracking()
            .FirstOrDefaultAsync(h => h.HypothesisId == hypothesisId, cancellationToken);

        return dbEntity != null ? HypothesisMapper.ToDomain(dbEntity) : null;
    }

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
