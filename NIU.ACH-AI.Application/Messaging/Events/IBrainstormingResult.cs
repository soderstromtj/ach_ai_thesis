using NIU.ACH_AI.Domain.Entities;

namespace NIU.ACH_AI.Application.Messaging.Events
{
    /// <summary>
    /// Serves as the completion payload for an initial theory generation phase.
    /// </summary>
    /// <remarks>
    /// Broadcast to transition the workflow state and provide the drafted theories to subsequent refinement phases.
    /// </remarks>
    public interface IBrainstormingResult
    {
        Guid ExperimentId { get; }
        Guid StepExecutionId { get; }
        List<Hypothesis> Hypotheses { get; }
        bool Success { get; }
        string? ErrorMessage { get; }
    }
}
