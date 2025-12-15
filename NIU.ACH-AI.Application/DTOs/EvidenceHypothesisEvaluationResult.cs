using NIU.ACH_AI.Domain.Entities;

namespace NIU.ACH_AI.Application.DTOs
{
    public class EvidenceHypothesisEvaluationResult
    {
        public List<EvidenceHypothesisEvaluation> Evaluations { get; set; } = new List<EvidenceHypothesisEvaluation>();
    }
}
