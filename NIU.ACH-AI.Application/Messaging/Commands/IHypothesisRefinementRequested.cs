using NIU.ACH_AI.Application.Configuration;
using NIU.ACH_AI.Application.DTOs;

namespace NIU.ACH_AI.Application.Messaging.Commands
{
    /// <summary>
    /// Command to initiate the Hypothesis Refinement step.
    /// </summary>
    public interface IHypothesisRefinementRequested
    {
         Guid ExperimentId { get; }
         Guid StepExecutionId { get; }
         OrchestrationPromptInput Input { get; }
         ACHStepConfiguration Configuration { get; }
         StepExecutionContext StepContext { get; }
         DateTime Timestamp { get; }
    }
}
