using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.Agents.Orchestration;
using Microsoft.SemanticKernel.Agents.Orchestration.Concurrent;
using Microsoft.SemanticKernel.Agents.Orchestration.GroupChat;
using Microsoft.SemanticKernel.Agents.Orchestration.Transforms;
using Microsoft.SemanticKernel.Agents.Runtime.InProcess;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using NIU.ACHAI.Application.Configuration;
using NIU.ACHAI.Application.Managers;
using NIU.ACHAI.Application.Models;
using NIU.ACHAI.Application.Services;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text;

namespace NIU.ACHAI.Application.Factories
{
#pragma warning disable SKEXP0110 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
    /// <summary>
    /// Base class for orchestration factories implementing the Template Method pattern.
    /// Provides common orchestration logic while allowing derived classes to customize specific behaviors.
    /// </summary>
    /// <typeparam name="TResult">The final result type (e.g., List&lt;Evidence&gt;, List&lt;Hypothesis&gt;)</typeparam>
    /// <typeparam name="TWrapper">The wrapper type for structured output (e.g., EvidenceResult, HypothesisResult)</typeparam>
    public abstract class BaseOrchestrationFactory<TResult, TWrapper> : IOrchestrationFactory<TResult>
        where TResult : class
        where TWrapper : class
    {
        protected readonly IAgentService _agentService;
        protected readonly IKernelBuilderService _kernelBuilderService;
        protected readonly OrchestrationSettings _orchestrationSettings;
        protected readonly ChatHistory _history;
        protected readonly ILogger _logger;
        protected readonly ILoggerFactory _loggerFactory;
        private int _currentTurn = 0;
        private string? _previousAgentName = null;
        private readonly Stopwatch _responseStopwatch = new Stopwatch();

        // Buffer streaming chunks per agent to allow assembling partials before final arrives.
        private readonly ConcurrentDictionary<string, StringBuilder> _streamBuffers = new();

        protected BaseOrchestrationFactory(
            IAgentService agentService,
            IKernelBuilderService kernelBuilderService,
            IOptions<OrchestrationSettings> orchestrationSettings,
            ILoggerFactory loggerFactory)
        {
            _agentService = agentService;
            _kernelBuilderService = kernelBuilderService;
            _orchestrationSettings = orchestrationSettings.Value;
            _history = new ChatHistory();
            _loggerFactory = loggerFactory;
            _logger = CreateLogger(loggerFactory);
        }

        public async Task<TResult> ExecuteCoreAsync(OrchestrationPromptInput input, CancellationToken cancellationToken = default)
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

            _logger.LogDebug($"Class: {GetType().Name}\tMessage: Setting up output transform settings. Expect an output of type {GetResultTypeName()}.");

            // Create structured output transform
            var outputTransform = new StructuredOutputTransform<TWrapper>(
                kernel.GetRequiredService<IChatCompletionService>(),
                new OpenAIPromptExecutionSettings
                {
                    ResponseFormat = typeof(TWrapper)
                });

            _logger.LogDebug($"Class: {GetType().Name}\tMessage: Creating orchestration object with {agents.Count()} agents.");

            // Allow derived classes to create their specific orchestration type
            AgentOrchestration<string, TWrapper> orchestration = CreateOrchestration(input, agentNames, kernel, agents.ToArray(), outputTransform);

            _logger.LogDebug($"Class: {GetType().Name}\tMessage: Starting in-process runtime that will execute the orchestration and manage state");
            var runtime = new InProcessRuntime();
            await runtime.StartAsync(cancellationToken);

            try
            {
                // Invoke orchestration with input and runtime context
                _logger.LogDebug($"Class: {GetType().Name}\tMessage: Invoking orchestration with input.");
                var result = await orchestration.InvokeAsync(input.ToString(), runtime, cancellationToken);

                _logger.LogDebug($"Class: {GetType().Name}\tMessage: Orchestration invocation completed. Processing result.");

                TResult? output = null;
                string? transformFailureReason = null;

                try
                {
                    // Attempt to get structured output with timeout
                    _logger.LogDebug($"Class: {GetType().Name}\tMessage: Attempting to retrieve structured output from orchestration result.");
                    var wrappedResult = await result.GetValueAsync(
                        TimeSpan.FromMinutes(_orchestrationSettings.TimeoutInMinutes),
                        cancellationToken);

                    if (wrappedResult == null)
                    {
                        transformFailureReason = "GetValueAsync returned null - structured output transformation likely failed";
                        _logger.LogError($"Class: {GetType().Name}\tMessage: {transformFailureReason}");
                    }
                    else
                    {
                        // Unwrap the result wrapper to get the actual result list
                        output = UnwrapResult(wrappedResult);
                        var itemCount = GetItemCount(output);
                        _logger.LogDebug($"Class: {GetType().Name}\tMessage: Successfully retrieved structured output with {itemCount} items.");
                    }
                }
                catch (TimeoutException tex)
                {
                    transformFailureReason = $"Timeout after {_orchestrationSettings.TimeoutInMinutes} minutes while waiting for structured output";
                    _logger.LogError(tex, $"Class: {GetType().Name}\tMessage: {transformFailureReason}");
                }
                catch (Exception ex)
                {
                    transformFailureReason = $"Exception during structured output transformation: {ex.GetType().Name} - {ex.Message}";
                    _logger.LogError(ex, $"Class: {GetType().Name}\tMessage: {transformFailureReason}");
                }

                // Return output with null safety
                if (output == null)
                {
                    return CreateEmptyResult();
                }

                return output;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Class: {GetType().Name}\tMessage: Exception during orchestration invocation: {ex.GetType().Name} - {ex.Message}");
                return CreateErrorResult();
            }
            finally
            {
                _logger.LogDebug($"Class: {GetType().Name}\tMessage: Stopping in-process runtime.");
                await runtime.RunUntilIdleAsync();
            }
        }

        #region Protected Callbacks
        protected async ValueTask StreamingResponseCallback(StreamingChatMessageContent response, bool isFinal)
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

        protected async ValueTask<ChatMessageContent> InteractiveCallback()
        {
            _logger.LogDebug($"Class: {GetType().Name}\tMessage: Interactive callback invoked - no user input provided, continuing orchestration.");
            return await ValueTask.FromResult(new ChatMessageContent
            {
                Content = "Continuing orchestration without user input."
            });
        }

        protected ValueTask ResponseCallback(ChatMessageContent response)
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
                    : GetAgentSelectionReason(_previousAgentName);

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
            _logger.LogDebug($"Class: {GetType().Name}\tMessage: Received response from agent '{agentName}' on turn {_currentTurn - 1} with content length {content.Length} characters{(tokenCount.HasValue ? $", {tokenCount.Value} tokens" : string.Empty)} in {responseDuration} ms.");

            return ValueTask.CompletedTask;
        }
        #endregion

        #region Abstract Methods - Template Method Pattern
        /// <summary>
        /// Creates the logger instance for this factory.
        /// </summary>
        protected abstract ILogger CreateLogger(ILoggerFactory loggerFactory);

        /// <summary>
        /// Creates the orchestration object specific to this factory type.
        /// Derived classes can create any type of AgentOrchestration (GroupChatOrchestration, ConcurrentOrchestration, etc.)
        /// and handle manager creation internally if needed.
        /// </summary>
        /// <param name="input">The orchestration prompt input</param>
        /// <param name="agentNames">List of agent names participating in the orchestration</param>
        /// <param name="kernel">The kernel instance for orchestration</param>
        /// <param name="agents">The array of agents participating in the orchestration</param>
        /// <param name="outputTransform">The structured output transform for result processing</param>
        /// <returns>A configured AgentOrchestration instance</returns>
        protected abstract AgentOrchestration<string, TWrapper> CreateOrchestration(
            OrchestrationPromptInput input,
            List<string> agentNames,
            Kernel kernel,
            Agent[] agents,
            StructuredOutputTransform<TWrapper> outputTransform);

        /// <summary>
        /// Gets the name of the result type for logging purposes.
        /// </summary>
        protected abstract string GetResultTypeName();

        /// <summary>
        /// Unwraps the structured output wrapper to get the actual result.
        /// </summary>
        protected abstract TResult UnwrapResult(TWrapper wrapper);

        /// <summary>
        /// Gets the count of items in the result for logging purposes.
        /// </summary>
        protected abstract int GetItemCount(TResult result);

        /// <summary>
        /// Creates an empty result when transformation fails.
        /// </summary>
        protected abstract TResult CreateEmptyResult();

        /// <summary>
        /// Creates an error result when orchestration fails.
        /// </summary>
        protected abstract TResult CreateErrorResult();

        /// <summary>
        /// Gets the reason for agent selection (e.g., "Round-robin selection" or custom manager name).
        /// </summary>
        protected abstract string GetAgentSelectionReason(string? previousAgentName);
        #endregion
    }
}
#pragma warning restore SKEXP0110 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
