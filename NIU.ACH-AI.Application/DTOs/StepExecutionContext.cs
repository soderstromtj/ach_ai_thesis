namespace NIU.ACH_AI.Application.DTOs
{
    public class StepExecutionContext
    {
        public Guid ExperimentId { get; set; }
        public Guid StepExecutionId { get; set; }
        public int AchStepId { get; set; }
        public string AchStepName { get; set; } = string.Empty;
        public IReadOnlyDictionary<string, Guid> AgentConfigurationIds { get; set; }
            = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);
    }
}
