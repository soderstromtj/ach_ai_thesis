namespace SemanticKernelPractice.Configuration
{
    /// <summary>
    /// Represents a single experiment configuration for ACH Step 2
    /// </summary>
    public class ACHStep2ExperimentSettings
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public ExperimentAIServiceSettings AIService { get; set; } = new();
        public AgentConfiguration[] AgentConfigurations { get; set; } = Array.Empty<AgentConfiguration>();
        public OrchestrationSettings OrchestrationSettings { get; set; } = new();
    }
}
