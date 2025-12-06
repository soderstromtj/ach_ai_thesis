using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.Agents.Orchestration.GroupChat;
using Microsoft.SemanticKernel.Agents.Orchestration.Transforms;
using Microsoft.SemanticKernel.Agents.Runtime.InProcess;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using SemanticKernelPractice.Configuration;
using SemanticKernelPractice.Models;
using SemanticKernelPractice.Services;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text;

namespace SemanticKernelPractice.Factories
{
#pragma warning disable SKEXP0110 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
    public class EvidenceExtractionOrchestrationFactory : IOrchestrationFactory<List<Evidence>>
    {
        private readonly IAgentService _agentService;
        private readonly IKernelBuilderService _kernelBuilderService;
        private readonly OrchestrationSettings _orchestrationSettings;
        private readonly ChatHistory _history;
        private readonly ILogger _logger;
        private int _currentTurn = 0;
        private string? _previousAgentName = null;
        private readonly Stopwatch _responseStopwatch = new Stopwatch();

        // Buffer streaming chunks per agent to allow assembling partials before final arrives.
        private readonly ConcurrentDictionary<string, StringBuilder> _streamBuffers = new();

        public EvidenceExtractionOrchestrationFactory(
            IAgentService agentService,
            IKernelBuilderService kernelBuilderService,
            IOptions<OrchestrationSettings> orchestrationSettings,
            ILoggerFactory loggerFactory)
        {
            _agentService = agentService;
            _kernelBuilderService = kernelBuilderService;
            _orchestrationSettings = orchestrationSettings.Value;
            _history = new ChatHistory();
            _logger = loggerFactory.CreateLogger<EvidenceExtractionOrchestrationFactory>();
        }

        public ChatHistory GetHistory() => _history;

        async Task<List<Evidence>> IOrchestrationFactory<List<Evidence>>.ExecuteCoreAsync(OrchestrationPromptInput input, CancellationToken cancellationToken)
        {
            // Create agents to be used in orchestration
            IEnumerable<Agent> agents = _agentService.CreateAgents();

            // Use .Where and .Select to filter out nulls and project to string
            var agentNames = agents
                .Select(a => a.Name)
                .Where(name => name != null)
                .Cast<string>()
                .ToList();

            // Build kernel for orchestration (different from agents' kernels)
            Kernel kernel = _kernelBuilderService.BuildKernel();

            _logger.LogDebug($"Class: {nameof(EvidenceExtractionOrchestrationFactory)}\tMessage: Setting up output transform settings. Expect an output of type {nameof(EvidenceResult)}.");

            // Use EvidenceResult wrapper - OpenAI structured output requires top-level object, not array
            var outputTransform = new StructuredOutputTransform<EvidenceResult>(
                kernel.GetRequiredService<IChatCompletionService>(),
                new OpenAIPromptExecutionSettings
                {
                    ResponseFormat = typeof(EvidenceResult)
                });

            var manager = new RoundRobinGroupChatManager
            {
                InteractiveCallback = InteractiveCallback,
                MaximumInvocationCount = _orchestrationSettings.MaximumInvocationCount,
            };

            _logger.LogDebug($"Class: {nameof(EvidenceExtractionOrchestrationFactory)}\tMessage: Creating {nameof(GroupChatOrchestration)} object with {agents.Count()} agents and {nameof(manager)} for the manager.");

            GroupChatOrchestration<string, EvidenceResult> orchestration = new GroupChatOrchestration<string, EvidenceResult>(manager, agents.ToArray())
            {
                ResponseCallback = ResponseCallback,
                ResultTransform = outputTransform.TransformAsync,
                StreamingResponseCallback = StreamingResponseCallback,
            };

            _logger.LogDebug($"Class: {nameof(EvidenceExtractionOrchestrationFactory)}\tMessage: Starting in-process runtime that will execute the orchestration and manage state");
            var runtime = new InProcessRuntime();
            await runtime.StartAsync(cancellationToken);

            try
            {
                // Invoke orchestration with input and runtime context
                _logger.LogDebug($"Class: {nameof(EvidenceExtractionOrchestrationFactory)}\tMessage: Invoking orchestration with input.");
                var result = await orchestration.InvokeAsync(input.ToString(), runtime, cancellationToken);

                _logger.LogDebug($"Class: {nameof(EvidenceExtractionOrchestrationFactory)}\tMessage: Orchestration invocation completed. Processing result.");

                List<Evidence>? output = null;
                string? transformFailureReason = null;

                try
                {
                    // Attempt to get structured output with timeout
                    _logger.LogDebug($"Class: {nameof(EvidenceExtractionOrchestrationFactory)}\tMessage: Attempting to retrieve structured output from orchestration result.");
                    var evidenceResult = await result.GetValueAsync(
                        TimeSpan.FromMinutes(_orchestrationSettings.TimeoutInMinutes),
                        cancellationToken);

                    if (evidenceResult == null)
                    {
                        transformFailureReason = "GetValueAsync returned null - structured output transformation likely failed";
                        _logger.LogError($"Class: {nameof(EvidenceExtractionOrchestrationFactory)}\tMessage: {transformFailureReason}");
                    }
                    else
                    {
                        // Unwrap the EvidenceResult to get List<Evidence>
                        output = evidenceResult.Evidence;
                        _logger.LogDebug($"Class: {nameof(EvidenceExtractionOrchestrationFactory)}\tMessage: Successfully retrieved structured output with {output.Count} evidence items.");
                    }
                }
                catch (TimeoutException tex)
                {
                    transformFailureReason = $"Timeout after {_orchestrationSettings.TimeoutInMinutes} minutes while waiting for structured output";
                    _logger.LogError(tex, $"Class: {nameof(EvidenceExtractionOrchestrationFactory)}\tMessage: {transformFailureReason}");
                }
                catch (Exception ex)
                {
                    transformFailureReason = $"Exception during structured output transformation: {ex.GetType().Name} - {ex.Message}";
                    _logger.LogError(ex, $"Class: {nameof(EvidenceExtractionOrchestrationFactory)}\tMessage: {transformFailureReason}");
                }

                // Return output with null safety
                if (output == null)
                {
                    return new List<Evidence>();
                }

                return output;
            }
            catch (Exception ex)
            {
                return new List<Evidence>
                {
                    new Evidence
                    {
                        Id = -1,
                        Description = "Error during orchestration",
                        Type = EvidenceType.Fact
                    }
                };
            }
            finally
            {
                _logger.LogDebug($"Class: {nameof(EvidenceExtractionOrchestrationFactory)}\tMessage: Stopping in-process runtime.");
                await runtime.RunUntilIdleAsync();
            }
        }

        #region Private Callbacks
        private async ValueTask StreamingResponseCallback(StreamingChatMessageContent response, bool isFinal)
        {
            var agentName = response.AuthorName ?? "Unknown";
            var chunk = response.Content ?? string.Empty;

            // Append chunk into per-agent buffer
            var buffer = _streamBuffers.GetOrAdd(agentName, _ => new StringBuilder());
            buffer.Append(chunk);

            // Show streaming output to console (simple live append)
            // Use Write rather than WriteLine so output appears as it streams.
            Console.Write(chunk);
            
            // When the orchestrator indicates final chunk, remove buffer (final response will be passed to ResponseCallback)
            if (isFinal)
            {
                // Optionally write a newline to finalize console output for this agent
                Console.WriteLine();

                // Clean up buffer to free memory; final insertion into history is handled by ResponseCallback
                _streamBuffers.TryRemove(agentName, out _);
            }

            await ValueTask.CompletedTask;
        }

        private async ValueTask<ChatMessageContent> InteractiveCallback()
        {
            _logger.LogDebug($"Class: {nameof(EvidenceExtractionOrchestrationFactory)}\tMessage: Interactive callback invoked - no user input provided, continuing orchestration.");
            return await ValueTask.FromResult(new ChatMessageContent
            {
                Content = "Continuing orchestration without user input."
            });
        }

        private ValueTask ResponseCallback(ChatMessageContent response)
        {
            _history.Add(response);
            _currentTurn++;

            var agentName = response.AuthorName ?? "Unknown";
            var content = response.Content ?? string.Empty;

            // Stop the previous response timer if it was running
            var responseDuration = _responseStopwatch.IsRunning ? _responseStopwatch.ElapsedMilliseconds : 0;
            _responseStopwatch.Stop();

            // Log agent selection (detect handoff)
            if (_previousAgentName != agentName)
            {
                var reason = _currentTurn == 1
                    ? "First agent in orchestration"
                    : $"Round-robin selection after {_previousAgentName}";

                _previousAgentName = agentName;
            }

            // Extract token count from metadata if available
            int? tokenCount = null;
            if (response.Metadata != null)
            {
                // Try to get token count from metadata dictionary directly
                if (response.Metadata.TryGetValue("OutputTokenCount", out var outputTokenCountObj) && outputTokenCountObj is int outputTokenCount)
                {
                    tokenCount = outputTokenCount;
                }
            }

            // Start timer for next response
            _responseStopwatch.Restart();

            // Future TODO: Store or process response metrics as needed
            _logger.LogDebug($"Class: {nameof(EvidenceExtractionOrchestrationFactory)}\tMessage: Received response from agent '{agentName}' on turn {_currentTurn - 1} with content length {content.Length} characters{(tokenCount.HasValue ? $", {tokenCount.Value} tokens" : string.Empty)} in {responseDuration} ms.");

            return ValueTask.CompletedTask;
        }
        #endregion

    }
}
#pragma warning restore SKEXP0110 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.