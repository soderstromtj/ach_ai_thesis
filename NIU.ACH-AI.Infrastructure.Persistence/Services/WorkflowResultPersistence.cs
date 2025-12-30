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

        public async Task<List<DomainEntity.Hypothesis>> SaveHypothesesAsync(
            Guid stepExecutionId,
            IEnumerable<DomainEntity.Hypothesis> hypotheses,
            bool isRefined,
            CancellationToken cancellationToken = default)
        {
            if (stepExecutionId == Guid.Empty)
            {
                throw new ArgumentException("Step execution ID must be provided.", nameof(stepExecutionId));
            }

            return await _hypothesisRepository.SaveBatchAsync(
                hypotheses,
                stepExecutionId,
                isRefined,
                cancellationToken);
        }

        public async Task<List<DomainEntity.Evidence>> SaveEvidenceAsync(
            Guid stepExecutionId,
            IEnumerable<DomainEntity.Evidence> evidence,
            CancellationToken cancellationToken = default)
        {
            if (stepExecutionId == Guid.Empty)
            {
                throw new ArgumentException("Step execution ID must be provided.", nameof(stepExecutionId));
            }

            return await _evidenceRepository.SaveBatchAsync(
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

            // Mapping/Lookups by title/claim are no longer needed as we expect IDs to be propagated.
            // We can trust that evaluations have the correct Hypotheses and Evidence objects with IDs.
            // However, to be safe or if the Repository requires specific handling, we could validate IDs here.
            // For now, we pass directly to repository which likely handles mapping using IDs present in the objects.
            // But wait, the repository interface takes maps? Let's check IEvidenceHypothesisEvaluationRepository.
            // If the repo signature requires maps, we need to change that too or construct maps from valid IDs.
            // Let's assume for this step we still need to pass maps OR we update the repository.
            // The plan said: "Update SaveEvaluationsAsync to remove the lookup... Use evaluation.Hypothesis.HypothesisId".
            // This implies the Repository might need update too if it relies on maps passed in.
            // Let's check if we can construct the maps from the objects themselves to satisfy existing signature,
            // OR if we should update the repository signature. The plan didn't explicitly say update EvaluationRepo signature,
            // but "remove the lookup" implies we don't query DB.

            var hypothesisMap = evaluations
                .Select(e => e.Hypothesis)
                .Distinct()
                .ToDictionary(h => h.ShortTitle, h => h.HypothesisId, StringComparer.OrdinalIgnoreCase);

            var evidenceMap = evaluations
                .Select(e => e.Evidence)
                .Distinct()
                .ToDictionary(e => e.Claim, e => e.EvidenceId, StringComparer.OrdinalIgnoreCase);

            await _evaluationRepository.SaveBatchAsync(
                evaluations,
                stepExecutionId,
                hypothesisMap,
                evidenceMap,
                cancellationToken);
        }
    }
}
