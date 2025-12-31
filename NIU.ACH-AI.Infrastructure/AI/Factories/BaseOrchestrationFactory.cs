using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.Agents.Orchestration;
using Microsoft.SemanticKernel.Agents.Orchestration.Transforms;
using Microsoft.SemanticKernel.Agents.Runtime.InProcess;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using NIU.ACH_AI.Application.Configuration;
using NIU.ACH_AI.Application.DTOs;
using NIU.ACH_AI.Application.Interfaces;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text;

namespace NIU.ACH_AI.Infrastructure.AI.Factories
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
        private readonly IAgentResponsePersistence? _agentResponsePersistence;
        private readonly ITokenUsageExtractor? _tokenUsageExtractor;
        private int _currentTurn = 0;
        private string? _previousAgentName = null;
        private readonly Stopwatch _responseStopwatch = new Stopwatch();
        private StepExecutionContext? _stepExecutionContext;

        // Buffer streaming chunks per agent to allow assembling partials before final arrives.
        private readonly ConcurrentDictionary<string, StringBuilder> _streamBuffers = new();

        protected BaseOrchestrationFactory(
            IAgentService agentService,
            IKernelBuilderService kernelBuilderService,
            IOptions<OrchestrationSettings> orchestrationSettings,
            ILoggerFactory loggerFactory,
            IAgentResponsePersistence? agentResponsePersistence = null,
            ITokenUsageExtractor? tokenUsageExtractor = null)
        {
            _agentService = agentService;
            _kernelBuilderService = kernelBuilderService;
            _orchestrationSettings = orchestrationSettings.Value;
            _history = new ChatHistory();
            _loggerFactory = loggerFactory;
            _logger = loggerFactory.CreateLogger(GetType());
            _agentResponsePersistence = agentResponsePersistence;
            _tokenUsageExtractor = tokenUsageExtractor;
        }

        /// <summary>
        /// Orchestrates the execution of an ACH step with the given input and returns the final result.
        /// </summary>
        /// <param name="input"></param>
        /// <param name="stepExecutionContext"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<TResult> ExecuteCoreAsync(
            OrchestrationPromptInput input,
            StepExecutionContext? stepExecutionContext = null,
            CancellationToken cancellationToken = default)
        {
            _stepExecutionContext = stepExecutionContext;
            if (_stepExecutionContext != null)
            {
                _logger.LogDebug(
                    "Class: {ClassName}\tMessage: Step execution context set. StepExecutionId: {StepExecutionId}.",
                    GetType().Name,
                    _stepExecutionContext.StepExecutionId);
            }

            // Create agents to be used in orchestration
            IEnumerable<Agent> agents = _agentService.CreateAgents();

            // Filter out null names and ensure unique list
            var agentNames = agents
                .Select(a => a.Name)
                .OfType<string>()
                .ToList();

            // Build kernel for orchestration (different from agents' kernels)
            Kernel kernel = _kernelBuilderService.BuildKernel();

            _logger.LogDebug(
                "Class: {ClassName}\tMessage: Setting up output transform settings. Expect an output of type {ResultType}.",
                GetType().Name,
                GetResultTypeName());

            // Create structured output transform
            var outputTransform = new StructuredOutputTransform<TWrapper>(
                kernel.GetRequiredService<IChatCompletionService>(),
                new OpenAIPromptExecutionSettings
                {
                    ResponseFormat = typeof(TWrapper)
                });

            _logger.LogDebug(
                "Class: {ClassName}\tMessage: Creating orchestration object with {AgentCount} agents.",
                GetType().Name,
                agents.Count());

            // Allow derived classes to create their specific orchestration type
            AgentOrchestration<string, TWrapper> orchestration = CreateOrchestration(input, agentNames, kernel, agents.ToArray(), outputTransform);

            _logger.LogDebug("Class: {ClassName}\tMessage: Starting in-process runtime that will execute the orchestration and manage state", GetType().Name);
            var runtime = new InProcessRuntime();
            await runtime.StartAsync(cancellationToken);

            try
            {
                // Invoke orchestration with input and runtime context
                _logger.LogDebug("Class: {ClassName}\tMessage: Invoking orchestration with input.", GetType().Name);
                var result = await orchestration.InvokeAsync(input.ToString(), runtime, cancellationToken);

                _logger.LogDebug("Class: {ClassName}\tMessage: Orchestration invocation completed. Processing result.", GetType().Name);

                TResult? output = null;
                string? transformFailureReason = null;

                try
                {
                    // Attempt to get structured output with timeout
                    _logger.LogDebug("Class: {ClassName}\tMessage: Attempting to retrieve structured output from orchestration result.", GetType().Name);
                    var wrappedResult = await result.GetValueAsync(
                        TimeSpan.FromMinutes(_orchestrationSettings.TimeoutInMinutes),
                        cancellationToken);

                    if (wrappedResult == null)
                    {
                        transformFailureReason = "GetValueAsync returned null - structured output transformation likely failed";
                        _logger.LogError("Class: {ClassName}\tMessage: {FailureReason}", GetType().Name, transformFailureReason);
                    }
                    else
                    {
                        // Unwrap the result wrapper to get the actual result list
                        output = UnwrapResult(wrappedResult);
                        var itemCount = GetItemCount(output);
                        _logger.LogDebug(
                            "Class: {ClassName}\tMessage: Successfully retrieved structured output with {ItemCount} items.",
                            GetType().Name,
                            itemCount);
                    }
                }
                catch (TimeoutException tex)
                {
                    transformFailureReason = $"Timeout after {_orchestrationSettings.TimeoutInMinutes} minutes while waiting for structured output";
                    _logger.LogError(tex, "Class: {ClassName}\tMessage: {FailureReason}", GetType().Name, transformFailureReason);
                }
                catch (Exception ex)
                {
                    transformFailureReason = $"Exception during structured output transformation: {ex.GetType().Name} - {ex.Message}";
                    _logger.LogError(ex, "Class: {ClassName}\tMessage: {FailureReason}", GetType().Name, transformFailureReason);
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
                _logger.LogError(
                    ex,
                    "Class: {ClassName}\tMessage: Exception during orchestration invocation: {ExceptionType} - {ExceptionMessage}",
                    GetType().Name,
                    ex.GetType().Name,
                    ex.Message);
                return CreateErrorResult();
            }
            finally
            {
                _logger.LogDebug("Class: {ClassName}\tMessage: Stopping in-process runtime.", GetType().Name);
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
            lock (buffer)
            {
                buffer.Append(chunk);
            }

            // Log streaming output instead of Console.Write
            if (_orchestrationSettings.StreamResponses)
            {
               _logger.LogTrace("Agent: {AgentName} Chunk: {Chunk}", agentName, chunk);
            }

            // When the orchestrator indicates final chunk
            if (isFinal)
            {
                // 1. Get full content
                string fullContent;
                lock (buffer)
                {
                    fullContent = buffer.ToString();
                }

                // 2. Persist response directly here, where metadata is available
                if (_agentResponsePersistence != null && _stepExecutionContext != null && response.Metadata != null)
                {
                    await PersistFromStreamingAsync(response, agentName, fullContent);
                }

                // Clean up buffer to free memory; final insertion into history is handled by ResponseCallback
                _streamBuffers.TryRemove(agentName, out _);
            }

            await ValueTask.CompletedTask;
        }

        private async Task PersistFromStreamingAsync(StreamingChatMessageContent response, string agentName, string content)
        {
             // We need response duration. StreamingResponseCallback doesn't track it cleanly per-turn like ResponseCallback.
            // We rely on the _responseStopwatch which is running.
            long responseDuration = _responseStopwatch.IsRunning ? _responseStopwatch.ElapsedMilliseconds : 0;
            
            await PersistAgentResponseInternalAsync(
                agentName,
                content,
                response.Metadata,
                responseDuration
            );
        }

        protected async ValueTask<ChatMessageContent> InteractiveCallback()
        {
            _logger.LogDebug("Class: {ClassName}\tMessage: Interactive callback invoked - no user input provided, continuing orchestration.", GetType().Name);
            return await ValueTask.FromResult(new ChatMessageContent
            {
                Content = "Continuing orchestration without user input."
            });
        }

        private readonly object _lockObject = new();

        protected async ValueTask ResponseCallback(ChatMessageContent response)
        {
            // Lock to ensure thread safety when multiple agents return simultaneously
            lock (_lockObject)
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

                // Extract token count from metadata if available for logging
                int? tokenCount = null;
                if (_tokenUsageExtractor != null)
                {
                    var usage = _tokenUsageExtractor.ExtractTokenUsage(response.Metadata);
                    tokenCount = usage.OutputTokenCount;
                }
                else if (response.Metadata?.TryGetValue("OutputTokenCount", out var outputTokenCountObj) == true && outputTokenCountObj is int outputTokenCount)
                {
                     // Fallback if extractor not provided
                    tokenCount = outputTokenCount;
                }

                // Start timer for next response
                _responseStopwatch.Restart();

                if (_orchestrationSettings.WriteResponses)
                {
                    _logger.LogInformation(
                        "\n[Turn {Turn}] Agent '{AgentName}' responded with {ContentLength} characters{TokenCountInfo} in {ResponseDuration} ms.\nContent: {Content}",
                        _currentTurn,
                        agentName,
                        content.Length,
                        tokenCount.HasValue ? $", {tokenCount.Value} tokens" : string.Empty,
                        responseDuration,
                        content);
                }

                _logger?.LogDebug(
                    "Class: {ClassName}\tMessage: Received response from agent '{AgentName}' on turn {Turn} with content length {ContentLength} characters{TokenCountInfo} in {ResponseDuration} ms.",
                    GetType().Name,
                    agentName,
                    _currentTurn - 1,
                    content.Length,
                    tokenCount.HasValue ? $", {tokenCount.Value} tokens" : string.Empty,
                    responseDuration);
            }

            return;
        }
        #endregion

        #region Abstract Methods - Template Method Pattern
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

        #region Private Helpers

        private async Task PersistAgentResponseInternalAsync(
            string agentName,
            string content,
            IReadOnlyDictionary<string, object?>? metadata,
            long responseDuration)
        {
            if (_stepExecutionContext == null || _agentResponsePersistence == null)
            {
                return;
            }

            if (!_stepExecutionContext.AgentConfigurationIds.TryGetValue(agentName, out var agentConfigurationId))
            {
                _logger?.LogWarning(
                    "Class: {ClassName}\tMessage: Agent configuration ID not found for agent '{AgentName}'. Skipping response persistence.",
                    GetType().Name,
                    agentName);
                return;
            }

            try
            {
                // Delegate all mapping and extraction logic to the enhanced persistence service
                await _agentResponsePersistence.SaveAgentResponseAsync(
                    content,
                    metadata,
                    agentName,
                    _stepExecutionContext.StepExecutionId,
                    agentConfigurationId,
                    _currentTurn,
                    responseDuration,
                    CancellationToken.None);
            }
            catch (Exception ex)
            {
                _logger?.LogError(
                    ex,
                    "Class: {ClassName}\tMessage: Failed to persist agent response for agent '{AgentName}'.",
                    GetType().Name,
                    agentName);
            }
        }
        #endregion
    }
}
#pragma warning restore SKEXP0110 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
