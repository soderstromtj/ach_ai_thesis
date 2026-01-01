using NIU.ACH_AI.Application.Configuration;
using NIU.ACH_AI.Application.DTOs;

namespace NIU.ACH_AI.Application.Messaging.Commands
{
    /// <summary>
    /// Command to initiate the Evidence-Hypothesis Evaluation step.
    /// </summary>
    public interface IEvidenceEvaluationRequested
    {
         Guid ExperimentId { get; }
         Guid StepExecutionId { get; }
         OrchestrationPromptInput Input { get; }
         ACHStepConfiguration Configuration { get; }
         StepExecutionContext StepContext { get; }
         Guid HypothesisStepExecutionId { get; }
         Guid EvidenceStepExecutionId { get; }
         DateTime Timestamp { get; }
    }
}
