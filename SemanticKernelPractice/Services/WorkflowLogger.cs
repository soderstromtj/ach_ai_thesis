using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SemanticKernelPractice.Configuration;
using SemanticKernelPractice.Models;
using System.Diagnostics;
using System.Text.Json;

namespace SemanticKernelPractice.Services
{
    /// <summary>
    /// Verbosity level for workflow logging.
    /// </summary>
    public enum LogVerbosity
    {
        /// <summary>Only log start and completion</summary>
        Minimal,

        /// <summary>Log agent selections and key decisions</summary>
        Standard,

        /// <summary>Log all events including full content</summary>
        Detailed,

        /// <summary>Log everything including debug information</summary>
        Debug
    }

    /// <summary>
    /// Centralized service for logging workflow orchestration events.
    /// </summary>
    public class WorkflowLogger
    {
        private readonly ILogger<WorkflowLogger> _logger;
        private readonly ConsoleFormatter _formatter;
        private readonly LogVerbosity _verbosity;
        private readonly bool _enableVisualization;
        private readonly bool _showFullContent;
        private readonly List<WorkflowEvent> _events;
        private readonly Stopwatch _stopwatch;
        private WorkflowSummary? _summary;

        public WorkflowLogger(
            ILogger<WorkflowLogger> logger,
            ConsoleFormatter formatter,
            IOptions<OrchestrationSettings> settings)
        {
            _logger = logger;
            _formatter = formatter;
            _verbosity = settings.Value.LogVerbosity;
            _enableVisualization = settings.Value.EnableWorkflowVisualization;
            _showFullContent = settings.Value.ShowFullResponseContent;
            _events = new List<WorkflowEvent>();
            _stopwatch = new Stopwatch();
        }

        /// <summary>
        /// Gets all logged events
        /// </summary>
        public IReadOnlyList<WorkflowEvent> Events => _events.AsReadOnly();

        /// <summary>
        /// Gets the workflow summary (available after completion)
        /// </summary>
        public WorkflowSummary? Summary => _summary;

        /// <summary>
        /// Logs the start of orchestration
        /// </summary>
        public void LogOrchestrationStart(string task, int maxTurns, int timeoutMinutes)
        {
            _stopwatch.Restart();

            var evt = new WorkflowEvent
            {
                EventType = WorkflowEventType.OrchestrationStarted,
                Content = task,
                Metadata = new Dictionary<string, object>
                {
                    ["MaxTurns"] = maxTurns,
                    ["TimeoutMinutes"] = timeoutMinutes
                }
            };

            _events.Add(evt);

            _logger.LogInformation("Orchestration started. MaxTurns={MaxTurns}, Timeout={Timeout}min",
                maxTurns, timeoutMinutes);

            if (_verbosity >= LogVerbosity.Minimal)
            {
                Console.Write(_formatter.FormatOrchestrationStart(task, maxTurns, timeoutMinutes));
            }
        }

        /// <summary>
        /// Logs agent selection
        /// </summary>
        public void LogAgentSelection(string agentName, string? reason, int turnNumber)
        {
            var evt = new WorkflowEvent
            {
                EventType = WorkflowEventType.AgentSelected,
                AgentName = agentName,
                Reason = reason,
                TurnNumber = turnNumber
            };

            _events.Add(evt);

            _logger.LogInformation("Agent selected: {AgentName} (Turn {TurnNumber}). Reason: {Reason}",
                agentName, turnNumber, reason ?? "None");

            if (_verbosity >= LogVerbosity.Standard)
            {
                Console.Write(_formatter.FormatAgentSelection(agentName, reason, turnNumber));
            }
        }

        /// <summary>
        /// Logs agent response
        /// </summary>
        public void LogAgentResponse(string agentName, string content, int? tokenCount = null, long? durationMs = null)
        {
            var evt = new WorkflowEvent
            {
                EventType = WorkflowEventType.AgentResponseReceived,
                AgentName = agentName,
                Content = content,
                TokenCount = tokenCount,
                DurationMs = durationMs,
                Metadata = new Dictionary<string, object>
                {
                    ["ShowFullContent"] = _showFullContent
                }
            };

            _events.Add(evt);

            _logger.LogInformation("Agent response received: {AgentName} ({TokenCount} tokens, {Duration}ms)",
                agentName, tokenCount ?? 0, durationMs ?? 0);

            if (_verbosity >= LogVerbosity.Detailed)
            {
                Console.Write(_formatter.FormatAgentResponse(agentName, content, tokenCount, durationMs, _showFullContent));
            }
            else if (_verbosity >= LogVerbosity.Standard)
            {
                // Show preview only
                Console.Write(_formatter.FormatAgentResponse(agentName, content, tokenCount, durationMs, false));
            }
        }

        /// <summary>
        /// Logs a handoff decision
        /// </summary>
        public void LogHandoff(string fromAgent, string toAgent, string? reason = null)
        {
            var evt = new WorkflowEvent
            {
                EventType = WorkflowEventType.HandoffDecision,
                AgentName = toAgent,
                Reason = reason,
                Metadata = new Dictionary<string, object>
                {
                    ["FromAgent"] = fromAgent
                }
            };

            _events.Add(evt);

            _logger.LogInformation("Handoff: {FromAgent} -> {ToAgent}. Reason: {Reason}",
                fromAgent, toAgent, reason ?? "None");

            if (_verbosity >= LogVerbosity.Standard)
            {
                Console.Write(_formatter.FormatHandoff(fromAgent, toAgent, reason));
            }
        }

        /// <summary>
        /// Logs a termination check
        /// </summary>
        public void LogTerminationCheck(bool shouldTerminate, string? reason = null)
        {
            var evt = new WorkflowEvent
            {
                EventType = WorkflowEventType.TerminationCheck,
                Reason = reason,
                Metadata = new Dictionary<string, object>
                {
                    ["ShouldTerminate"] = shouldTerminate
                }
            };

            _events.Add(evt);

            _logger.LogInformation("Termination check: {ShouldTerminate}. Reason: {Reason}",
                shouldTerminate, reason ?? "None");

            if (_verbosity >= LogVerbosity.Debug)
            {
                Console.Write(_formatter.FormatTerminationCheck(shouldTerminate, reason));
            }
        }

        /// <summary>
        /// Logs result filtering
        /// </summary>
        public void LogResultFiltering(string reason, int resultCount)
        {
            var evt = new WorkflowEvent
            {
                EventType = WorkflowEventType.ResultFiltered,
                Reason = reason,
                Metadata = new Dictionary<string, object>
                {
                    ["ResultCount"] = resultCount
                }
            };

            _events.Add(evt);

            _logger.LogInformation("Results filtered: {Reason}. Count: {Count}", reason, resultCount);

            if (_verbosity >= LogVerbosity.Debug)
            {
                Console.WriteLine($"  Result Filtering: {reason} (Count: {resultCount})");
            }
        }

        /// <summary>
        /// Logs an error
        /// </summary>
        public void LogError(string message, Exception? exception = null)
        {
            var evt = new WorkflowEvent
            {
                EventType = WorkflowEventType.Error,
                Content = message,
                Metadata = exception != null ? new Dictionary<string, object>
                {
                    ["StackTrace"] = exception.StackTrace ?? ""
                } : null
            };

            _events.Add(evt);

            _logger.LogError(exception, "Workflow error: {Message}", message);

            if (_verbosity >= LogVerbosity.Minimal)
            {
                Console.Write(_formatter.FormatError(message, exception?.StackTrace));
            }
        }

        /// <summary>
        /// Logs the completion of orchestration and generates summary
        /// </summary>
        public void LogOrchestrationComplete(string terminationReason, int? resultCount = null)
        {
            _stopwatch.Stop();

            // Build summary
            _summary = new WorkflowSummary
            {
                StartTime = _events.FirstOrDefault()?.Timestamp ?? DateTime.UtcNow,
                EndTime = DateTime.UtcNow,
                TerminationReason = terminationReason,
                ResultCount = resultCount
            };

            // Calculate statistics from events
            var responseEvents = _events.Where(e => e.EventType == WorkflowEventType.AgentResponseReceived).ToList();
            _summary.TotalTurns = responseEvents.Count;
            _summary.TotalTokens = responseEvents.Sum(e => e.TokenCount ?? 0);

            foreach (var responseEvent in responseEvents)
            {
                if (responseEvent.AgentName != null)
                {
                    if (!_summary.TurnsByAgent.ContainsKey(responseEvent.AgentName))
                    {
                        _summary.TurnsByAgent[responseEvent.AgentName] = 0;
                        _summary.TokensByAgent[responseEvent.AgentName] = 0;
                    }

                    _summary.TurnsByAgent[responseEvent.AgentName]++;
                    _summary.TokensByAgent[responseEvent.AgentName] += responseEvent.TokenCount ?? 0;
                }
            }

            var evt = new WorkflowEvent
            {
                EventType = WorkflowEventType.OrchestrationCompleted,
                Content = terminationReason,
                DurationMs = _stopwatch.ElapsedMilliseconds
            };

            _events.Add(evt);

            _logger.LogInformation(
                "Orchestration completed. Duration={Duration}ms, Turns={Turns}, Tokens={Tokens}, Reason={Reason}",
                _summary.DurationMs, _summary.TotalTurns, _summary.TotalTokens, terminationReason);

            if (_verbosity >= LogVerbosity.Minimal)
            {
                Console.Write(_formatter.FormatOrchestrationComplete(_summary));
            }
        }

        /// <summary>
        /// Exports events to JSON format
        /// </summary>
        public string ExportToJson()
        {
            var export = new
            {
                Summary = _summary,
                Events = _events
            };

            return JsonSerializer.Serialize(export, new JsonSerializerOptions
            {
                WriteIndented = true
            });
        }

        /// <summary>
        /// Saves events to a JSON file
        /// </summary>
        public async Task SaveToFileAsync(string filePath)
        {
            var json = ExportToJson();
            await File.WriteAllTextAsync(filePath, json);

            _logger.LogInformation("Workflow events saved to {FilePath}", filePath);
        }

        /// <summary>
        /// Generates a Mermaid sequence diagram of the workflow
        /// </summary>
        public string GenerateMermaidDiagram()
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("sequenceDiagram");
            sb.AppendLine("    participant O as Orchestrator");

            // Get unique agents
            var agents = _events
                .Where(e => !string.IsNullOrEmpty(e.AgentName))
                .Select(e => e.AgentName!)
                .Distinct()
                .ToList();

            foreach (var agent in agents)
            {
                var safeAgentName = agent.Replace(" ", "");
                sb.AppendLine($"    participant {safeAgentName} as {agent}");
            }

            sb.AppendLine();

            // Add events
            foreach (var evt in _events)
            {
                switch (evt.EventType)
                {
                    case WorkflowEventType.OrchestrationStarted:
                        sb.AppendLine("    Note over O: Orchestration Started");
                        break;

                    case WorkflowEventType.AgentSelected:
                        if (evt.AgentName != null)
                        {
                            var safeAgentName = evt.AgentName.Replace(" ", "");
                            sb.AppendLine($"    O->>+{safeAgentName}: Select Agent (Turn {evt.TurnNumber})");
                        }
                        break;

                    case WorkflowEventType.AgentResponseReceived:
                        if (evt.AgentName != null)
                        {
                            var safeAgentName = evt.AgentName.Replace(" ", "");
                            var preview = evt.Content?.Length > 50
                                ? evt.Content.Substring(0, 50) + "..."
                                : evt.Content ?? "";
                            sb.AppendLine($"    {safeAgentName}-->>-O: {preview}");
                        }
                        break;

                    case WorkflowEventType.OrchestrationCompleted:
                        sb.AppendLine("    Note over O: Orchestration Completed");
                        break;
                }
            }

            return sb.ToString();
        }
    }
}
