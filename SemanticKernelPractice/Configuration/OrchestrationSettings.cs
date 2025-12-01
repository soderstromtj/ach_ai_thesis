namespace SemanticKernelPractice.Configuration
{
    public class OrchestrationSettings
    {
        public int MaximumInvocationCount { get; set; } = 30;
        public int TimeoutInMinutes { get; set; } = 15;
    }
}