using Microsoft.EntityFrameworkCore;
using NIU.ACH_AI.Domain.Entities;
using NIU.ACH_AI.Infrastructure.Persistence.Mappers;
using NIU.ACH_AI.Infrastructure.Persistence.Models;

namespace NIU.ACH_AI.Infrastructure.Persistence.Repositories;

/// <summary>
/// Implementation of evidence-hypothesis evaluation repository using Entity Framework Core.
/// Handles mapping between domain and database entities internally.
/// </summary>
public class EvidenceHypothesisEvaluationRepository : IEvidenceHypothesisEvaluationRepository
{
    private readonly AchAIDbContext _context;

    public EvidenceHypothesisEvaluationRepository(AchAIDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task SaveBatchAsync(
        IEnumerable<EvidenceHypothesisEvaluation> evaluations,
        Guid stepExecutionId,
        Dictionary<string, Guid> hypothesisIdMap,
        Dictionary<string, Guid> evidenceIdMap,
        CancellationToken cancellationToken = default)
    {
        if (evaluations == null || !evaluations.Any())
            return;

        var dbEntities = new List<Models.EvidenceHypothesisEvaluation>();

        foreach (var evaluation in evaluations)
        {
            // Look up the persisted IDs using the business keys
            if (!hypothesisIdMap.TryGetValue(evaluation.Hypothesis.ShortTitle, out var hypothesisId))
            {
                throw new InvalidOperationException(
                    $"Hypothesis '{evaluation.Hypothesis.ShortTitle}' not found in ID map. " +
                    $"Ensure hypothesis is persisted before saving evaluations.");
            }

            if (!evidenceIdMap.TryGetValue(evaluation.Evidence.Claim, out var evidenceId))
            {
                throw new InvalidOperationException(
                    $"Evidence '{evaluation.Evidence.Claim}' not found in ID map. " +
                    $"Ensure evidence is persisted before saving evaluations.");
            }

            // Map to database entity
            var dbEntity = EvidenceHypothesisEvaluationMapper.ToDatabase(
                evaluation,
                stepExecutionId,
                hypothesisId,
                evidenceId);

            dbEntities.Add(dbEntity);
        }

        // Save to database
        await _context.EvidenceHypothesisEvaluations.AddRangeAsync(dbEntities, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<List<EvidenceHypothesisEvaluation>> GetByStepExecutionIdAsync(
        Guid stepExecutionId,
        CancellationToken cancellationToken = default)
    {
        var dbEntities = await _context.EvidenceHypothesisEvaluations
            .Include(e => e.Hypothesis)
            .Include(e => e.Evidence)
            .Where(e => e.StepExecutionId == stepExecutionId)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return EvidenceHypothesisEvaluationMapper.ToDomain(dbEntities);
    }

    public async Task<List<EvidenceHypothesisEvaluation>> GetByHypothesisIdAsync(
        Guid hypothesisId,
        CancellationToken cancellationToken = default)
    {
        var dbEntities = await _context.EvidenceHypothesisEvaluations
            .Include(e => e.Hypothesis)
            .Include(e => e.Evidence)
            .Where(e => e.HypothesisId == hypothesisId)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return EvidenceHypothesisEvaluationMapper.ToDomain(dbEntities);
    }

    public async Task<List<EvidenceHypothesisEvaluation>> GetByEvidenceIdAsync(
        Guid evidenceId,
        CancellationToken cancellationToken = default)
    {
        var dbEntities = await _context.EvidenceHypothesisEvaluations
            .Include(e => e.Hypothesis)
            .Include(e => e.Evidence)
            .Where(e => e.EvidenceId == evidenceId)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return EvidenceHypothesisEvaluationMapper.ToDomain(dbEntities);
    }
}
