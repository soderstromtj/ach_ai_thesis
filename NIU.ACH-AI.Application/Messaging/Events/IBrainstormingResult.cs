using NIU.ACH_AI.Domain.Entities;

namespace NIU.ACH_AI.Application.Messaging.Events
{
    /// <summary>
    /// Contract for the result of a brainstorming request.
    /// </summary>
    public interface IBrainstormingResult
    {
        Guid ExperimentId { get; }
        Guid StepExecutionId { get; }
        List<Hypothesis> Hypotheses { get; }
        bool Success { get; }
        string? ErrorMessage { get; }
    }
}
