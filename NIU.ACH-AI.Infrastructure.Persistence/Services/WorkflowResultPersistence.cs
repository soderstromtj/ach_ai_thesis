using Microsoft.EntityFrameworkCore;
using NIU.ACH_AI.Application.Interfaces;
using NIU.ACH_AI.Infrastructure.Persistence.Repositories;
using DbModel = NIU.ACH_AI.Infrastructure.Persistence.Models;
using DomainEntity = NIU.ACH_AI.Domain.Entities;

namespace NIU.ACH_AI.Infrastructure.Persistence.Services
{
    /// <summary>
    /// Persists workflow outputs (hypotheses, evidence, evaluations) to the database.
    /// </summary>
    public class WorkflowResultPersistence : IWorkflowResultPersistence
    {
        private readonly DbModel.AchAIDbContext _context;
        private readonly IHypothesisRepository _hypothesisRepository;
        private readonly IEvidenceRepository _evidenceRepository;
        private readonly IEvidenceHypothesisEvaluationRepository _evaluationRepository;

        public WorkflowResultPersistence(
            DbModel.AchAIDbContext context,
            IHypothesisRepository hypothesisRepository,
            IEvidenceRepository evidenceRepository,
            IEvidenceHypothesisEvaluationRepository evaluationRepository)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _hypothesisRepository = hypothesisRepository ?? throw new ArgumentNullException(nameof(hypothesisRepository));
            _evidenceRepository = evidenceRepository ?? throw new ArgumentNullException(nameof(evidenceRepository));
            _evaluationRepository = evaluationRepository ?? throw new ArgumentNullException(nameof(evaluationRepository));
        }

        public async Task SaveHypothesesAsync(
            Guid stepExecutionId,
            IEnumerable<DomainEntity.Hypothesis> hypotheses,
            bool isRefined,
            CancellationToken cancellationToken = default)
        {
            if (stepExecutionId == Guid.Empty)
            {
                throw new ArgumentException("Step execution ID must be provided.", nameof(stepExecutionId));
            }

            await _hypothesisRepository.SaveBatchAsync(
                hypotheses,
                stepExecutionId,
                isRefined,
                cancellationToken);
        }

        public async Task SaveEvidenceAsync(
            Guid stepExecutionId,
            IEnumerable<DomainEntity.Evidence> evidence,
            CancellationToken cancellationToken = default)
        {
            if (stepExecutionId == Guid.Empty)
            {
                throw new ArgumentException("Step execution ID must be provided.", nameof(stepExecutionId));
            }

            await _evidenceRepository.SaveBatchAsync(
                evidence,
                stepExecutionId,
                cancellationToken);
        }

        public async Task SaveEvaluationsAsync(
            Guid stepExecutionId,
            IEnumerable<DomainEntity.EvidenceHypothesisEvaluation> evaluations,
            Guid hypothesisStepExecutionId,
            Guid evidenceStepExecutionId,
            CancellationToken cancellationToken = default)
        {
            if (stepExecutionId == Guid.Empty)
            {
                throw new ArgumentException("Step execution ID must be provided.", nameof(stepExecutionId));
            }

            if (hypothesisStepExecutionId == Guid.Empty)
            {
                throw new ArgumentException("Hypothesis step execution ID must be provided.", nameof(hypothesisStepExecutionId));
            }

            if (evidenceStepExecutionId == Guid.Empty)
            {
                throw new ArgumentException("Evidence step execution ID must be provided.", nameof(evidenceStepExecutionId));
            }

            var hypothesisMap = await _context.Hypotheses
                .AsNoTracking()
                .Where(h => h.StepExecutionId == hypothesisStepExecutionId)
                .GroupBy(h => h.ShortTitle)
                .ToDictionaryAsync(
                    g => g.Key,
                    g => g.First().HypothesisId,
                    StringComparer.OrdinalIgnoreCase,
                    cancellationToken);

            var evidenceMap = await _context.Evidences
                .AsNoTracking()
                .Where(e => e.StepExecutionId == evidenceStepExecutionId)
                .GroupBy(e => e.Claim)
                .ToDictionaryAsync(
                    g => g.Key,
                    g => g.First().EvidenceId,
                    StringComparer.OrdinalIgnoreCase,
                    cancellationToken);

            await _evaluationRepository.SaveBatchAsync(
                evaluations,
                stepExecutionId,
                hypothesisMap,
                evidenceMap,
                cancellationToken);
        }
    }
}
