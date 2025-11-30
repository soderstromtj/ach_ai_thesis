namespace SemanticKernelPractice.Configuration
{
    public class OrchestrationSettings
    {
        public int MaximumInvocationCount { get; set; } = 5;
        public int TimeoutInMinutes { get; set; } = 2;
    }
}