using Microsoft.EntityFrameworkCore;
using NIU.ACH_AI.Domain.Entities;
using NIU.ACH_AI.Infrastructure.Persistence.Data;
using NIU.ACH_AI.Infrastructure.Persistence.Mappers;

namespace NIU.ACH_AI.Infrastructure.Persistence.Repositories
{
    /// <summary>
    /// Repository implementation for Hypothesis persistence operations
    /// </summary>
    public class HypothesisRepository : IHypothesisRepository
    {
        private readonly AchDbContext _context;

        public HypothesisRepository(AchDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        /// <summary>
        /// Saves a batch of hypotheses to the database
        /// </summary>
        public async Task<int> SaveBatchAsync(
            IEnumerable<Hypothesis> hypotheses,
            Guid stepExecutionId,
            bool isRefined,
            CancellationToken cancellationToken = default)
        {
            if (hypotheses == null)
            {
                throw new ArgumentNullException(nameof(hypotheses));
            }

            if (stepExecutionId == Guid.Empty)
            {
                throw new ArgumentException("StepExecutionId cannot be empty", nameof(stepExecutionId));
            }

            var hypothesesList = hypotheses.ToList();
            if (!hypothesesList.Any())
            {
                return 0;
            }

            var entities = hypothesesList
                .Select(h => HypothesisMapper.ToDatabase(h, stepExecutionId, isRefined))
                .ToList();

            await _context.Hypotheses.AddRangeAsync(entities, cancellationToken);
            return await _context.SaveChangesAsync(cancellationToken);
        }

        /// <summary>
        /// Gets all hypotheses for a specific step execution
        /// </summary>
        public async Task<IEnumerable<Hypothesis>> GetByStepExecutionIdAsync(
            Guid stepExecutionId,
            CancellationToken cancellationToken = default)
        {
            if (stepExecutionId == Guid.Empty)
            {
                throw new ArgumentException("StepExecutionId cannot be empty", nameof(stepExecutionId));
            }

            var entities = await _context.Hypotheses
                .Where(h => h.StepExecutionId == stepExecutionId)
                .OrderBy(h => h.CreatedAt)
                .ToListAsync(cancellationToken);

            return entities.Select(HypothesisMapper.ToDomain);
        }

        /// <summary>
        /// Gets all refined hypotheses for a specific step execution
        /// </summary>
        public async Task<IEnumerable<Hypothesis>> GetRefinedByStepExecutionIdAsync(
            Guid stepExecutionId,
            CancellationToken cancellationToken = default)
        {
            if (stepExecutionId == Guid.Empty)
            {
                throw new ArgumentException("StepExecutionId cannot be empty", nameof(stepExecutionId));
            }

            var entities = await _context.Hypotheses
                .Where(h => h.StepExecutionId == stepExecutionId && h.IsRefined)
                .OrderBy(h => h.CreatedAt)
                .ToListAsync(cancellationToken);

            return entities.Select(HypothesisMapper.ToDomain);
        }
    }
}
