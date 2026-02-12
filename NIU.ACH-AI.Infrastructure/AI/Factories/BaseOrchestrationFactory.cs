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
        private int _currentTurn = 0;
        private string? _previousAgentName = null;

        private StepExecutionContext? _stepExecutionContext;

        // Buffer streaming chunks per agent to allow assembling partials before final arrives.
        private readonly ConcurrentDictionary<string, StringBuilder> _streamBuffers = new();

        /// <summary>
        /// Initializes a new instance of the <see cref="BaseOrchestrationFactory{TResult, TWrapper}"/> class.
        /// </summary>
        /// <param name="agentService">Service for creating agents.</param>
        /// <param name="kernelBuilderService">Service for building semantic kernels.</param>
        /// <param name="orchestrationSettings">Settings for orchestration execution.</param>
        /// <param name="loggerFactory">Logger factory.</param>
        /// <param name="agentResponsePersistence">Optional service for persisting agent responses.</param>
        protected BaseOrchestrationFactory(
            IAgentService agentService,
            IKernelBuilderService kernelBuilderService,
            IOptions<OrchestrationSettings> orchestrationSettings,
            ILoggerFactory loggerFactory,
            IAgentResponsePersistence? agentResponsePersistence = null)
        {
            _agentService = agentService;
            _kernelBuilderService = kernelBuilderService;
            _orchestrationSettings = orchestrationSettings.Value;
            _history = new ChatHistory();
            _loggerFactory = loggerFactory;
            _logger = loggerFactory.CreateLogger(GetType());
            _agentResponsePersistence = agentResponsePersistence;
        }

        /// <summary>
        /// Orchestrates the execution of an ACH step with the given input and returns the final result.
        /// </summary>
        /// <param name="input">The input prompt and context for orchestration.</param>
        /// <param name="stepExecutionContext">The execution context for tracking step status.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The result of the orchestration execution.</returns>
        public async Task<TResult> ExecuteCoreAsync(
            OrchestrationPromptInput input,
            StepExecutionContext? stepExecutionContext = null,
            CancellationToken cancellationToken = default)
        {
            _stepExecutionContext = stepExecutionContext;
            _history.Clear(); // Ensure fresh history for this execution
            if (_stepExecutionContext != null)
            {
                _logger.LogDebug(
                    "Class: {ClassName}\tMessage: Step execution context set. StepExecutionId: {StepExecutionId}.",
                    GetType().Name,
                    _stepExecutionContext.StepExecutionId);
            }

            // Create agents and optional persist configuration
            var (agentListEnumerable, configIds) = _agentService.CreateAgents(_stepExecutionContext?.StepExecutionId);
            var agentList = agentListEnumerable.ToList();

             // Update context with configuration IDs returned by service
             if (_stepExecutionContext != null && configIds.Count > 0)
             {
                 foreach(var kvp in configIds)
                 {
                     _stepExecutionContext.AgentConfigurationIds[kvp.Key] = kvp.Value;
                 }
             }

            // Build kernel for orchestration (different from agents' kernels)
            Kernel kernel = _kernelBuilderService.BuildKernel();

            _logger.LogDebug(
                "Class: {ClassName}\tMessage: Setting up output transform settings. Expect an output of type {ResultType}.",
                GetType().Name,
                GetResultTypeName());


            var outputTransform = new StructuredOutputTransform<TWrapper>(
                kernel.GetRequiredService<IChatCompletionService>(),
                new OpenAIPromptExecutionSettings
                {
                    ResponseFormat = typeof(TWrapper)
                });

            _logger.LogDebug(
                "Class: {ClassName}\tMessage: Creating orchestration object with {AgentCount} agents.",
                GetType().Name,
                agentList.Count);

            // Allow derived classes to create their specific orchestration type
            AgentOrchestration<string, TWrapper> orchestration = CreateOrchestration(input, kernel, agentList.ToArray(), outputTransform);

            _logger.LogDebug("Class: {ClassName}\tMessage: Starting in-process runtime that will execute the orchestration and manage state", GetType().Name);
            var runtime = new InProcessRuntime();
            await runtime.StartAsync(cancellationToken);

            try
            {

                _logger.LogDebug("Class: {ClassName}\tMessage: Invoking orchestration with input.", GetType().Name);
                var result = await orchestration.InvokeAsync(input.ToString(), runtime, cancellationToken);

                _logger.LogDebug("Class: {ClassName}\tMessage: Orchestration invocation completed. Processing result.", GetType().Name);

                TResult? output = null;
                string? transformFailureReason = null;

                try
                {

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

            if (isFinal)
            {
                string fullContent;
                lock (buffer)
                {
                    fullContent = buffer.ToString();
                }

                // Persist response directly here since metadata is available in the streaming callback
                if (_agentResponsePersistence != null && _stepExecutionContext != null && response.Metadata != null)
                {
                    await PersistAgentResponseInternalAsync(agentName, fullContent, response.Metadata);
                }

                // Clean up buffer to free memory; final insertion into history is handled by ResponseCallback
                _streamBuffers.TryRemove(agentName, out _);
            }

            await ValueTask.CompletedTask;
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

                // Log agent selection (detect handoff)
                if (_previousAgentName != agentName)
                {
                    var reason = _currentTurn == 1
                        ? "First agent in orchestration"
                        : GetAgentSelectionReason(_previousAgentName);

                    _previousAgentName = agentName;
                }

                if (_orchestrationSettings.WriteResponses)
                {
                    _logger.LogInformation(
                        "\n[Turn {Turn}] Agent '{AgentName}' responded with {ContentLength} characters.\nContent: {Content}",
                        _currentTurn,
                        agentName,
                        content.Length,
                        content);
                }

                _logger?.LogDebug(
                    "Class: {ClassName}\tMessage: Received response from agent '{AgentName}' on turn {Turn} with content length {ContentLength} characters.",
                    GetType().Name,
                    agentName,
                    _currentTurn - 1,
                    content.Length);
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
            IReadOnlyDictionary<string, object?>? metadata)
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
                    0, // Response duration not tracked
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
