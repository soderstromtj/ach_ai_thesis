using Microsoft.Extensions.Logging;

namespace NIU.ACHAI.Application.Configuration
{
    public class OrchestrationSettings
    {
        public int MaximumInvocationCount { get; set; } = 10;
        public int TimeoutInMinutes { get; set; } = 15;
    }
}