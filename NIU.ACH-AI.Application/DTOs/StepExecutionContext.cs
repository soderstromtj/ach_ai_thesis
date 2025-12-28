namespace NIU.ACH_AI.Application.DTOs
{
    public class StepExecutionContext
    {
        public Guid ExperimentId { get; init; }
        public Guid StepExecutionId { get; init; }
        public int AchStepId { get; init; }
        public string AchStepName { get; init; } = string.Empty;
    }
}
