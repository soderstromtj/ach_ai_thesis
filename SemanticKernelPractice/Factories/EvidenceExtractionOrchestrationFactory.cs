using Azure.AI.Agents.Persistent;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.Agents.Orchestration;
using Microsoft.SemanticKernel.Agents.Orchestration.GroupChat;
using Microsoft.SemanticKernel.Agents.Orchestration.Transforms;
using Microsoft.SemanticKernel.Agents.Runtime.InProcess;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using SemanticKernelPractice.Configuration;
using SemanticKernelPractice.Managers;
using SemanticKernelPractice.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using SemanticKernelPractice.Models;

namespace SemanticKernelPractice.Factories
{
#pragma warning disable SKEXP0110 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
    public class EvidenceExtractionOrchestrationFactory : IOrchestrationFactory<List<Evidence>>
    {
        private readonly IAgentService _agentService;
        private readonly IKernelBuilderService _kernelBuilderService;
        private readonly OrchestrationSettings _orchestrationSettings;
        private readonly WorkflowLogger _workflowLogger;
        private readonly ChatHistory _history;
        private int _currentTurn = 0;
        private string? _previousAgentName = null;
        private readonly Stopwatch _responseStopwatch = new Stopwatch();

        public EvidenceExtractionOrchestrationFactory(
            IAgentService agentService,
            IKernelBuilderService kernelBuilderService,
            IOptions<OrchestrationSettings> orchestrationSettings,
            WorkflowLogger workflowLogger)
        {
            _agentService = agentService;
            _kernelBuilderService = kernelBuilderService;
            _orchestrationSettings = orchestrationSettings.Value;
            _workflowLogger = workflowLogger;
            _history = new ChatHistory();
        }

        async Task<List<Evidence>> IOrchestrationFactory<List<Evidence>>.ExecuteCoreAsync(string input, CancellationToken cancellationToken)
        {
            // Log orchestration start
            _workflowLogger.LogOrchestrationStart(
                "Extract evidence for ACH analysis",
                _orchestrationSettings.MaximumInvocationCount,
                _orchestrationSettings.TimeoutInMinutes);

            Agent[] agents = _agentService.CreateAgents().ToArray();

            if (agents.Count() < 3)
            {
                var errorMessage = "At least three agents are required for evidence extraction orchestration.";
                _workflowLogger.LogError(errorMessage);
                throw new InvalidOperationException(errorMessage);
            }

            // Build kernel for output transformation
            Kernel kernel = _kernelBuilderService.BuildKernel();

            var outputTransform = new StructuredOutputTransform<List<Evidence>>(
                kernel.GetRequiredService<IChatCompletionService>(),
                new OpenAIPromptExecutionSettings
                {
                    ResponseFormat = typeof(List<Evidence>)
                });


            var manager = new RoundRobinGroupChatManager
            {
                MaximumInvocationCount = _orchestrationSettings.MaximumInvocationCount,
            };


            GroupChatOrchestration<string, List<Evidence>> orchestration = new GroupChatOrchestration<string, List<Evidence>>(manager, agents)
            {
                ResponseCallback = ResponseCallback,
                ResultTransform = outputTransform.TransformAsync
            };

            var runtime = new InProcessRuntime();
            await runtime.StartAsync(cancellationToken);

            try
            {
                var result = await orchestration.InvokeAsync(input, runtime, cancellationToken);

                var output = await result.GetValueAsync(TimeSpan.FromMinutes(_orchestrationSettings.TimeoutInMinutes), cancellationToken);

                // Log successful completion
                var terminationReason = _currentTurn >= _orchestrationSettings.MaximumInvocationCount
                    ? "Maximum invocation count reached"
                    : "Orchestration completed successfully";

                _workflowLogger.LogOrchestrationComplete(terminationReason, output?.Count);

                // Save to file if configured
                if (_orchestrationSettings.SaveWorkflowToFile)
                {
                    var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                    var filename = Path.Combine(_orchestrationSettings.WorkflowLogDirectory, $"workflow_{timestamp}.json");
                    Directory.CreateDirectory(_orchestrationSettings.WorkflowLogDirectory);
                    await _workflowLogger.SaveToFileAsync(filename);
                }

                return output;
            }
            catch (Exception ex)
            {
                _workflowLogger.LogError($"Orchestration error: {ex.Message}", ex);

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
                await runtime.RunUntilIdleAsync();
            }
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

                _workflowLogger.LogAgentSelection(agentName, reason, _currentTurn);

                // Log handoff if not the first turn
                if (_currentTurn > 1 && _previousAgentName != null)
                {
                    _workflowLogger.LogHandoff(_previousAgentName, agentName, "Round-robin manager selected next agent");
                }

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

            // Log the agent response with full content
            _workflowLogger.LogAgentResponse(agentName, content, tokenCount, responseDuration);

            return ValueTask.CompletedTask;
        }

        public ChatHistory GetHistory() => _history;
    }
}
#pragma warning restore SKEXP0110 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.