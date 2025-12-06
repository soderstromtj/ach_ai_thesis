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
using SemanticKernelPractice.Managers;
using SemanticKernelPractice.Models;
using SemanticKernelPractice.Services;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text;

namespace SemanticKernelPractice.Factories
{
#pragma warning disable SKEXP0110 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
    public class HypothesisGenerationOrchestrationFactory : BaseOrchestrationFactory<List<Hypothesis>, HypothesisResult>
    {

        private int _currentTurn = 0;
        private string? _previousAgentName = null;
        private readonly Stopwatch _responseStopwatch = new Stopwatch();

        // Buffer streaming chunks per agent to allow assembling partials before final arrives.
        private readonly ConcurrentDictionary<string, StringBuilder> _streamBuffers = new();

        public HypothesisGenerationOrchestrationFactory(
            IAgentService agentService,
            IKernelBuilderService kernelBuilderService,
            IOptions<OrchestrationSettings> orchestrationSettings,
            ILoggerFactory loggerFactory)
            : base(agentService, kernelBuilderService, orchestrationSettings, loggerFactory)
        { }

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
            _logger.LogTrace($"Class: {nameof(HypothesisGenerationOrchestrationFactory)}\tMessage: Interactive callback invoked - no user input provided, continuing orchestration.");
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
                    : $"{nameof(HypothesisGenerationGroupChatManager)} selection after {_previousAgentName}";

                _previousAgentName = agentName;
            }

            // Extract token count from metadata if available
            int? tokenCount = null;
            if (response.Metadata != null)
            {
                // Try to get token count from metadata dictionary directly
                if (response.Metadata.TryGetValue("TotalTokenCount", out var outputTokenCountObj) && outputTokenCountObj is int outputTokenCount)
                {
                    tokenCount = outputTokenCount;
                }
            }

            // Start timer for next response
            _responseStopwatch.Restart();

            // Future TODO: Store or process response metrics as needed
            _logger.LogTrace($"Class: {nameof(HypothesisGenerationOrchestrationFactory)}\tMessage: Received response from agent '{agentName}' on turn {_currentTurn - 1} with content length {content.Length} characters{(tokenCount.HasValue ? $", {tokenCount.Value} tokens" : string.Empty)} in {responseDuration} ms.");

            return ValueTask.CompletedTask;
        }

        protected override ILogger CreateLogger(ILoggerFactory loggerFactory)
        {
            return loggerFactory.CreateLogger<EvidenceExtractionOrchestrationFactory>();
        }

        protected override GroupChatManager CreateManager(OrchestrationPromptInput input, List<string> agentNames, Kernel kernel, IGroupChatPromptStrategy? promptStrategy, AgentParticipationTracker? agentParticipationTracker)
        {
            var manager = new HypothesisGenerationGroupChatManager(
                input,
                agentNames,
                kernel.GetRequiredService<IChatCompletionService>(),
                new HypothesisGenerationPromptStrategy(),
                new AgentParticipationTracker(),
                _loggerFactory.CreateLogger<HypothesisGenerationGroupChatManager>())
            {
                InteractiveCallback = InteractiveCallback,
                MaximumInvocationCount = _orchestrationSettings.MaximumInvocationCount
            };

            return manager;
        }

        protected override string GetResultTypeName()
        {
            return nameof(EvidenceResult);
        }

        protected override List<Hypothesis> UnwrapResult(HypothesisResult wrapper)
        {
            return wrapper.Hypotheses;
        }

        protected override int GetItemCount(List<Hypothesis> result)
        {
            return result.Count;
        }

        protected override List<Hypothesis> CreateEmptyResult()
        {
            return new List<Hypothesis>();
        }

        protected override List<Hypothesis> CreateErrorResult()
        {
            return new List<Hypothesis>
            {
                new Hypothesis
                {
                    Title = "Error during orchestration"
                }
            };
        }

        protected override string GetAgentSelectionReason(string? previousAgentName)
        {
            return $"Round-robin selection after {previousAgentName}";
        }
    }
}
#pragma warning restore SKEXP0110 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.