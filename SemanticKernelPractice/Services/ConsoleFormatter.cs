using SemanticKernelPractice.Models;
using System.Text;

namespace SemanticKernelPractice.Services
{
    /// <summary>
    /// Provides formatted console output for workflow events and summaries.
    /// </summary>
    public class ConsoleFormatter
    {
        private const int DEFAULT_WIDTH = 80;
        private const int CONTENT_PREVIEW_LENGTH = 200;

        /// <summary>
        /// Formats the orchestration start event
        /// </summary>
        public string FormatOrchestrationStart(string task, int maxTurns, int timeoutMinutes)
        {
            var sb = new StringBuilder();
            sb.AppendLine();
            sb.AppendLine(CreateSeparator('='));
            sb.AppendLine("  ORCHESTRATION STARTED");
            sb.AppendLine(CreateSeparator('='));
            sb.AppendLine($"  Max Turns: {maxTurns}");
            sb.AppendLine($"  Timeout: {timeoutMinutes} minutes");
            sb.AppendLine(CreateSeparator('='));
            sb.AppendLine();
            return sb.ToString();
        }

        /// <summary>
        /// Formats an agent selection event
        /// </summary>
        public string FormatAgentSelection(string agentName, string? reason, int turnNumber)
        {
            var sb = new StringBuilder();
            sb.AppendLine();
            sb.AppendLine(CreateSeparator('-', $" Turn {turnNumber} "));
            sb.AppendLine($"  Agent Selected: {agentName}");
            if (!string.IsNullOrWhiteSpace(reason))
            {
                sb.AppendLine($"  Reason: {reason}");
            }
            sb.AppendLine(CreateSeparator('-'));
            return sb.ToString();
        }

        /// <summary>
        /// Formats an agent response event
        /// </summary>
        public string FormatAgentResponse(string agentName, string content, int? tokenCount = null, long? durationMs = null, bool showFullContent = true)
        {
            var sb = new StringBuilder();
            sb.AppendLine();
            sb.AppendLine($"  [{agentName}]");

            if (tokenCount.HasValue || durationMs.HasValue)
            {
                var metrics = new List<string>();
                if (tokenCount.HasValue)
                    metrics.Add($"{tokenCount} tokens");
                if (durationMs.HasValue)
                    metrics.Add($"{durationMs}ms");
                sb.AppendLine($"  ({string.Join(", ", metrics)})");
            }

            sb.AppendLine();

            if (showFullContent)
            {
                // Show full content with proper indentation
                var lines = content.Split('\n');
                foreach (var line in lines)
                {
                    sb.AppendLine($"  {line}");
                }
            }
            else
            {
                // Show preview only
                var preview = content.Length > CONTENT_PREVIEW_LENGTH
                    ? content.Substring(0, CONTENT_PREVIEW_LENGTH) + "..."
                    : content;
                sb.AppendLine($"  {preview}");
            }

            sb.AppendLine();
            return sb.ToString();
        }

        /// <summary>
        /// Formats a handoff decision event
        /// </summary>
        public string FormatHandoff(string fromAgent, string toAgent, string? reason = null)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"  Handoff: {fromAgent} -> {toAgent}");
            if (!string.IsNullOrWhiteSpace(reason))
            {
                sb.AppendLine($"  Reason: {reason}");
            }
            return sb.ToString();
        }

        /// <summary>
        /// Formats a termination check event
        /// </summary>
        public string FormatTerminationCheck(bool shouldTerminate, string? reason = null)
        {
            var sb = new StringBuilder();
            sb.AppendLine();
            sb.AppendLine($"  Termination Check: {(shouldTerminate ? "YES" : "NO")}");
            if (!string.IsNullOrWhiteSpace(reason))
            {
                sb.AppendLine($"  Reason: {reason}");
            }
            return sb.ToString();
        }

        /// <summary>
        /// Formats the orchestration completion event
        /// </summary>
        public string FormatOrchestrationComplete(WorkflowSummary summary)
        {
            var sb = new StringBuilder();
            sb.AppendLine();
            sb.AppendLine(CreateSeparator('='));
            sb.AppendLine("  ORCHESTRATION COMPLETED");
            sb.AppendLine(CreateSeparator('='));
            sb.AppendLine($"  Total Turns: {summary.TotalTurns}");
            sb.AppendLine($"  Duration: {FormatDuration(summary.DurationMs)}");
            if (summary.TotalTokens > 0)
            {
                sb.AppendLine($"  Total Tokens: {summary.TotalTokens:N0}");
            }
            if (!string.IsNullOrWhiteSpace(summary.TerminationReason))
            {
                sb.AppendLine($"  Termination: {summary.TerminationReason}");
            }
            if (summary.ResultCount.HasValue)
            {
                sb.AppendLine($"  Results: {summary.ResultCount} items extracted");
            }

            if (summary.TurnsByAgent.Any())
            {
                sb.AppendLine();
                sb.AppendLine("  Turns by Agent:");
                foreach (var kvp in summary.TurnsByAgent.OrderByDescending(x => x.Value))
                {
                    sb.AppendLine($"    - {kvp.Key}: {kvp.Value}");
                }
            }

            if (summary.TokensByAgent.Any())
            {
                sb.AppendLine();
                sb.AppendLine("  Tokens by Agent:");
                foreach (var kvp in summary.TokensByAgent.OrderByDescending(x => x.Value))
                {
                    sb.AppendLine($"    - {kvp.Key}: {kvp.Value:N0}");
                }
            }

            sb.AppendLine(CreateSeparator('='));
            sb.AppendLine();
            return sb.ToString();
        }

        /// <summary>
        /// Formats an error event
        /// </summary>
        public string FormatError(string message, string? stackTrace = null)
        {
            var sb = new StringBuilder();
            sb.AppendLine();
            sb.AppendLine(CreateSeparator('!', " ERROR "));
            sb.AppendLine($"  {message}");
            if (!string.IsNullOrWhiteSpace(stackTrace))
            {
                sb.AppendLine();
                sb.AppendLine("  Stack Trace:");
                var lines = stackTrace.Split('\n');
                foreach (var line in lines.Take(10)) // Limit stack trace lines
                {
                    sb.AppendLine($"    {line}");
                }
            }
            sb.AppendLine(CreateSeparator('!'));
            sb.AppendLine();
            return sb.ToString();
        }

        /// <summary>
        /// Creates a separator line with optional centered text
        /// </summary>
        private string CreateSeparator(char character, string? centerText = null, int width = DEFAULT_WIDTH)
        {
            if (string.IsNullOrEmpty(centerText))
            {
                return new string(character, width);
            }

            int textLength = centerText.Length;
            int paddingTotal = width - textLength;
            int paddingLeft = paddingTotal / 2;
            int paddingRight = paddingTotal - paddingLeft;

            return new string(character, paddingLeft) + centerText + new string(character, paddingRight);
        }

        /// <summary>
        /// Formats a duration in milliseconds to a human-readable string
        /// </summary>
        private string FormatDuration(long durationMs)
        {
            if (durationMs < 1000)
                return $"{durationMs}ms";
            if (durationMs < 60000)
                return $"{durationMs / 1000.0:F1}s";

            var minutes = durationMs / 60000;
            var seconds = (durationMs % 60000) / 1000.0;
            return $"{minutes}m {seconds:F1}s";
        }

        /// <summary>
        /// Formats a generic workflow event
        /// </summary>
        public string FormatEvent(WorkflowEvent evt)
        {
            return evt.EventType switch
            {
                WorkflowEventType.OrchestrationStarted => FormatOrchestrationStart(
                    evt.Content ?? "Unknown task",
                    evt.Metadata?.ContainsKey("MaxTurns") == true ? (int)evt.Metadata["MaxTurns"] : 0,
                    evt.Metadata?.ContainsKey("TimeoutMinutes") == true ? (int)evt.Metadata["TimeoutMinutes"] : 0),

                WorkflowEventType.AgentSelected => FormatAgentSelection(
                    evt.AgentName ?? "Unknown",
                    evt.Reason,
                    evt.TurnNumber ?? 0),

                WorkflowEventType.AgentResponseReceived => FormatAgentResponse(
                    evt.AgentName ?? "Unknown",
                    evt.Content ?? "",
                    evt.TokenCount,
                    evt.DurationMs,
                    evt.Metadata?.ContainsKey("ShowFullContent") == true && (bool)evt.Metadata["ShowFullContent"]),

                WorkflowEventType.HandoffDecision => FormatHandoff(
                    evt.Metadata?.ContainsKey("FromAgent") == true ? (string)evt.Metadata["FromAgent"] : "Unknown",
                    evt.AgentName ?? "Unknown",
                    evt.Reason),

                WorkflowEventType.TerminationCheck => FormatTerminationCheck(
                    evt.Metadata?.ContainsKey("ShouldTerminate") == true && (bool)evt.Metadata["ShouldTerminate"],
                    evt.Reason),

                WorkflowEventType.Error => FormatError(
                    evt.Content ?? "An error occurred",
                    evt.Metadata?.ContainsKey("StackTrace") == true ? (string)evt.Metadata["StackTrace"] : null),

                _ => $"[{evt.EventType}] {evt.Content}"
            };
        }
    }
}
