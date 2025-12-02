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
using System.Diagnostics;

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

            // Use EvidenceResult wrapper - OpenAI structured output requires top-level object, not array
            var outputTransform = new StructuredOutputTransform<EvidenceResult>(
                kernel.GetRequiredService<IChatCompletionService>(),
                new OpenAIPromptExecutionSettings
                {
                    Temperature = 0.7,
                    ResponseFormat = typeof(EvidenceResult)
                });


            var manager = new RoundRobinGroupChatManager
            {
                InteractiveCallback = InteractiveCallback,
                MaximumInvocationCount = _orchestrationSettings.MaximumInvocationCount,
            };


            GroupChatOrchestration<string, EvidenceResult> orchestration = new GroupChatOrchestration<string, EvidenceResult>(manager, agents)
            {
                ResponseCallback = ResponseCallback,
                ResultTransform = outputTransform.TransformAsync
            };

            // Create in-process runtime that will execute the orchestration and manage state
            var runtime = new InProcessRuntime();
            await runtime.StartAsync(cancellationToken);

            try
            {
                // Invoke orchestration with input and runtime context
                var result = await orchestration.InvokeAsync(input, runtime, cancellationToken);

                // Log structured output transformation attempt
                _workflowLogger.LogStructuredOutputStart("EvidenceResult (List<Evidence>)", _orchestrationSettings.TimeoutInMinutes);

                List<Evidence>? output = null;
                string? transformFailureReason = null;
                bool transformSucceeded = false;

                try
                {
                    // Attempt to get structured output with timeout
                    var evidenceResult = await result.GetValueAsync(
                        TimeSpan.FromMinutes(_orchestrationSettings.TimeoutInMinutes),
                        cancellationToken);

                    if (evidenceResult == null)
                    {
                        transformFailureReason = "GetValueAsync returned null - structured output transformation likely failed";
                        _workflowLogger.LogStructuredOutputResult(false, transformFailureReason);
                    }
                    else
                    {
                        // Unwrap the EvidenceResult to get List<Evidence>
                        output = evidenceResult.Evidence;
                        transformSucceeded = true;
                        _workflowLogger.LogStructuredOutputResult(true, resultCount: output?.Count ?? 0);
                    }
                }
                catch (TimeoutException tex)
                {
                    transformFailureReason = $"Timeout after {_orchestrationSettings.TimeoutInMinutes} minutes while waiting for structured output";
                    _workflowLogger.LogStructuredOutputResult(false, transformFailureReason);
                    _workflowLogger.LogError($"Structured output timeout: {tex.Message}", tex);
                }
                catch (Exception ex)
                {
                    transformFailureReason = $"Exception during structured output transformation: {ex.GetType().Name} - {ex.Message}";
                    _workflowLogger.LogStructuredOutputResult(false, transformFailureReason);
                    _workflowLogger.LogError($"Structured output transformation error: {ex.Message}", ex);
                }

                // Determine termination reason
                string terminationReason;
                if (!transformSucceeded)
                {
                    terminationReason = transformFailureReason ?? "Structured output transformation failed";
                }
                else if (_currentTurn >= _orchestrationSettings.MaximumInvocationCount)
                {
                    terminationReason = "Maximum invocation count reached";
                }
                else
                {
                    terminationReason = "Orchestration completed successfully";
                }

                _workflowLogger.LogOrchestrationComplete(terminationReason, output?.Count);

                // Save to file if configured
                if (_orchestrationSettings.SaveWorkflowToFile)
                {
                    var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                    var filename = Path.Combine(_orchestrationSettings.WorkflowLogDirectory, $"workflow_{timestamp}.json");
                    Directory.CreateDirectory(_orchestrationSettings.WorkflowLogDirectory);
                    await _workflowLogger.SaveToFileAsync(filename);
                }

                // Return output with null safety
                if (output == null)
                {
                    _workflowLogger.LogError("Returning empty list due to null output from structured transformation");
                    return new List<Evidence>();
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

        private async ValueTask<ChatMessageContent> InteractiveCallback()
        {
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