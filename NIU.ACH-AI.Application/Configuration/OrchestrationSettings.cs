namespace NIU.ACH_AI.Application.Configuration
{
    /// <summary>
    /// Dictates the execution limits and behavioral rules for a specific automation phase.
    /// </summary>
    /// <remarks>
    /// Essential for preventing infinite loops in autonomous interactions and managing resource consumption.
    /// </remarks>
    public class OrchestrationSettings
    {
        /// <summary>
        /// Gets or sets the upper boundary for conversational turns or API calls to prevent runaway loops.
        /// </summary>
        public int MaximumInvocationCount { get; set; } = 10;

        /// <summary>
        /// Gets or sets the temporal limit for the entire phase to guarantee a finite execution time.
        /// </summary>
        public int TimeoutInMinutes { get; set; } = 15;

        /// <summary>
        /// Gets or sets a value indicating whether intermediate outputs are emitted live to connected clients.
        /// </summary>
        public bool StreamResponses { get; set; } = false;

        /// <summary>
        /// Gets or sets a value indicating whether outputs should be persisted to the database for historical auditing.
        /// </summary>
        public bool WriteResponses { get; set; } = true;
    }
}