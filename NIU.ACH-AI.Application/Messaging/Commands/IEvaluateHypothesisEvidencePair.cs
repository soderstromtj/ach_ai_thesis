using NIU.ACH_AI.Application.Configuration;
using NIU.ACH_AI.Application.DTOs;
using NIU.ACH_AI.Domain.Entities;

namespace NIU.ACH_AI.Application.Messaging.Commands
{
    public interface IEvaluateHypothesisEvidencePair
    {
        Guid ExperimentId { get; }
        Guid StepExecutionId { get; }
        Guid HypothesisStepExecutionId { get; }
        Guid EvidenceStepExecutionId { get; }
        ACHStepConfiguration Configuration { get; }
        OrchestrationPromptInput Input { get; } // Contains specific Hypothesis and Evidence
        StepExecutionContext StepContext { get; }
    }
}
