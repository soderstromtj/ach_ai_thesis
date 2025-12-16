namespace NIU.ACH_AI.Application.Configuration
{
    public class OrchestrationSettings
    {
        public int MaximumInvocationCount { get; set; } = 10;
        public int TimeoutInMinutes { get; set; } = 15;
        public bool StreamResponses { get; set; } = false;
        public bool WriteResponses { get; set; } = true;
    }
}