namespace NIU.ACH_AI.Application.Configuration
{
    /// <summary>
    /// Configures the behavior of the orchestration engine for a specific step.
    /// </summary>
    public class OrchestrationSettings
    {
        /// <summary>
        /// Gets or sets the maximum number of invocations or turns allowed.
        /// </summary>
        public int MaximumInvocationCount { get; set; } = 10;

        /// <summary>
        /// Gets or sets the timeout duration in minutes for the orchestration process.
        /// </summary>
        public int TimeoutInMinutes { get; set; } = 15;

        /// <summary>
        /// Gets or sets a value indicating whether responses should be streamed.
        /// </summary>
        public bool StreamResponses { get; set; } = false;

        /// <summary>
        /// Gets or sets a value indicating whether responses should be persisted to storage.
        /// </summary>
        public bool WriteResponses { get; set; } = true;
    }
}