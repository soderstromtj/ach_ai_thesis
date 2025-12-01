using SemanticKernelPractice.Services;

namespace SemanticKernelPractice.Configuration
{
    public class OrchestrationSettings
    {
        public int MaximumInvocationCount { get; set; } = 30;
        public int TimeoutInMinutes { get; set; } = 15;

        /// <summary>
        /// Verbosity level for workflow logging
        /// </summary>
        public LogVerbosity LogVerbosity { get; set; } = LogVerbosity.Standard;

        /// <summary>
        /// Enable workflow visualization (Mermaid diagrams, JSON export)
        /// </summary>
        public bool EnableWorkflowVisualization { get; set; } = true;

        /// <summary>
        /// Show full agent response content (true) or truncated preview (false)
        /// </summary>
        public bool ShowFullResponseContent { get; set; } = true;

        /// <summary>
        /// Automatically save workflow events to file after completion
        /// </summary>
        public bool SaveWorkflowToFile { get; set; } = false;

        /// <summary>
        /// Directory path for saving workflow logs
        /// </summary>
        public string WorkflowLogDirectory { get; set; } = "./workflow_logs";
    }
}