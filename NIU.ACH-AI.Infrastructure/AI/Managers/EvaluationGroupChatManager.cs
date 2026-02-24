using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents.Orchestration.GroupChat;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using NIU.ACH_AI.Application.DTOs;
using NIU.ACH_AI.Application.Exceptions;
using NIU.ACH_AI.Application.Interfaces;
using NIU.ACH_AI.Application.Configuration;
using System.Text.Json;
using System.Diagnostics;

namespace NIU.ACH_AI.Infrastructure.AI.Managers
{
#pragma warning disable SKEXP0110 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
    /// <summary>
    /// Manages the conversation flow between agents during the hypothesis generation step.
    /// </summary>
    public class EvaluationGroupChatManager : GroupChatManager
    {
        private readonly OrchestrationPromptInput _input;
        private readonly IEnumerable<string> _agentNames;
        private readonly IChatCompletionService _chatCompletion;
        private readonly IGroupChatPromptStrategy _promptStrategy;
        private readonly AgentParticipationTracker _participationTracker;
        private readonly ILogger<EvaluationGroupChatManager> _logger;

        /// <summary>
        /// Sets up the group chat manager for hypothesis generation.
        /// </summary>
        /// <param name="input">The orchestration prompt input containing the key question and context.</param>
        /// <param name="agentNames">The configurations of all agents participating in the group chat.</param>
        /// <param name="chatCompletion">The chat completion service for LLM interactions.</param>
        /// <param name="promptStrategy">The strategy for generating prompts.</param>
        /// <param name="participationTracker">The tracker for monitoring agent participation.</param>
        /// <param name="logger">The logger for diagnostic information.</param>
        public EvaluationGroupChatManager(
            OrchestrationPromptInput input,
            IEnumerable<string> agentNames,
            IChatCompletionService chatCompletion,
            IGroupChatPromptStrategy promptStrategy,
            AgentParticipationTracker participationTracker,
            ILogger<EvaluationGroupChatManager> logger)
        {
            ArgumentNullException.ThrowIfNull(input);
            ArgumentNullException.ThrowIfNull(agentNames);
            ArgumentNullException.ThrowIfNull(chatCompletion);
            ArgumentNullException.ThrowIfNull(promptStrategy);
            ArgumentNullException.ThrowIfNull(participationTracker);
            ArgumentNullException.ThrowIfNull(logger);

            var configsList = agentNames.ToList();
            if (configsList.Count == 0)
            {
                throw new ArgumentException("At least one agent configuration is required", nameof(agentNames));
            }

            _input = input;
            _agentNames = agentNames;
            _chatCompletion = chatCompletion;
            _promptStrategy = promptStrategy;
            _participationTracker = participationTracker;
            _logger = logger;

            _logger.LogInformation(
                "HypothesisGenerationGroupChatManager created with {AgentCount} agents and max limit of {MaxLimit}",
                _agentNames.Count(),
                MaximumInvocationCount > 0 ? MaximumInvocationCount.ToString() : "unlimited");
        }

        /// <summary>
        /// Asks the LLM to format the final answers into a JSON object.
        /// </summary>
        /// <param name="history">The chat history containing the conversation.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A result containing the filtered and formatted hypotheses.</returns>
        public override async ValueTask<GroupChatManagerResult<string>> FilterResults(
            ChatHistory history,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Filtering results from chat history with {MessageCount} messages", history.Count);

            string prompt = _promptStrategy.GetFilterPrompt(_input);
            _logger.LogDebug("Filter prompt generated. Length: {Length}", prompt.Length);

            var stopwatch = Stopwatch.StartNew();
            var response = await GetResponseAsync<string>(history, prompt, cancellationToken);
            stopwatch.Stop();

            _logger.LogInformation("FilterResults completed in {Elapsed}ms. Result length: {Length}", 
                stopwatch.ElapsedMilliseconds, 
                response.Value?.Length ?? 0);

            return response;
        }

        /// <summary>
        /// Asks the LLM to choose the next agent to speak.
        /// </summary>
        public override async ValueTask<GroupChatManagerResult<string>> SelectNextAgent(
            ChatHistory history,
            GroupChatTeam team,
            CancellationToken cancellationToken = default)
        {
            int turnCount = GetTurnCount(history);
            _logger.LogInformation("Selecting next agent. Turn count: {TurnCount}", turnCount);

            string prompt = _promptStrategy.GetSelectionPrompt(_input, team.Keys);

            var stopwatch = Stopwatch.StartNew();
            var result = await GetResponseAsync<string>(history, prompt, cancellationToken);
            stopwatch.Stop();

            _logger.LogInformation("SelectNextAgent completed in {Elapsed}ms. Selected: {Agent}", 
                stopwatch.ElapsedMilliseconds, 
                result.Value);

            return result;
        }

        /// <summary>
        /// Tells the chat manager whether to wait for user input.
        /// </summary>
        /// <param name="history">The chat history containing the conversation.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A result indicating that user input is not needed for automated workflows.</returns>
        public override ValueTask<GroupChatManagerResult<bool>> ShouldRequestUserInput(
            ChatHistory history,
            CancellationToken cancellationToken = default)
        {
            _logger.LogTrace("User input not required for automated ACH workflow");

            return ValueTask.FromResult(
                new GroupChatManagerResult<bool>(false)
                {
                    Reason = "Automated ACH evidence extraction workflow - no user input needed."
                });
        }

        /// <summary>
        /// Decides if the conversation should stop based on limits or LLM assessment.
        /// </summary>
        /// <param name="history">The chat history containing the conversation.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A result indicating whether the chat should terminate and the reason.</returns>
        public override async ValueTask<GroupChatManagerResult<bool>> ShouldTerminate(
            ChatHistory history,
            CancellationToken cancellationToken = default)
        {
            int turnCount = GetTurnCount(history);

            _logger.LogInformation(
                "Evaluating termination. Turn count: {TurnCount}, Max: {MaxLimit}",
                turnCount,
                MaximumInvocationCount > 0 ? MaximumInvocationCount.ToString() : "unlimited");

            // Check if maximum invocation limit has been reached
            if (HasReachedMaximumLimit(turnCount))
            {
                _logger.LogInformation(
                    "Terminating: Maximum invocation limit of {Limit} reached",
                    MaximumInvocationCount);

                return new GroupChatManagerResult<bool>(true)
                {
                    Reason = $"Maximum invocation limit of {MaximumInvocationCount} reached."
                };
            }

            // Check if all agents have participated
            if (HaveAllAgentsParticipated(history))
            {
                _logger.LogInformation("All agents have participated at least once");
            }
            else
            {
                _logger.LogInformation("Not all agents have participated yet; continuing the chat");
                return new GroupChatManagerResult<bool>(false)
                {
                    Reason = "Not all agents have participated yet."
                };
            }

            // Delegate to LLM for quality assessment
            return await EvaluateTerminationCriteria(history, cancellationToken);
        }

        #region Private Methods

        /// <summary>
        /// Counts how many messages have been sent by agents.
        /// </summary>
        /// <param name="history">The chat history to analyze.</param>
        /// <returns>The number of assistant messages in the history.</returns>
        private int GetTurnCount(ChatHistory history)
        {
            return history.Count(msg => msg.Role == AuthorRole.Assistant);
        }

        /// <summary>
        /// Checks if the chat has hit the maximum allowed message limit.
        /// </summary>
        /// <param name="turnCount">The current turn count.</param>
        /// <returns>True if the limit has been reached; otherwise, false.</returns>
        private bool HasReachedMaximumLimit(int turnCount)
        {
            return MaximumInvocationCount > 0 && turnCount >= MaximumInvocationCount;
        }

        /// <summary>
        /// Checks if all agents have spoken.
        /// </summary>
        /// <param name="history">The chat history to analyze.</param>
        /// <returns>True if all agents have participated; otherwise, false.</returns>
        private bool HaveAllAgentsParticipated(ChatHistory history)
        {
            return _participationTracker.HaveAllAgentsParticipated(history, _agentNames);
        }

        /// <summary>
        /// Asks the LLM if the conversation has met its goals and should stop.
        /// </summary>
        /// <param name="history">The chat history to analyze.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A result indicating whether to terminate based on hypothesis quality.</returns>
        private async ValueTask<GroupChatManagerResult<bool>> EvaluateTerminationCriteria(
            ChatHistory history,
            CancellationToken cancellationToken)
        {
            _logger.LogDebug("Delegating termination decision to LLM for quality assessment");

            string prompt = _promptStrategy.GetTerminationPrompt(_input, _agentNames);
            var responseTask = await GetResponseAsync<bool>(history, prompt, cancellationToken);

            _logger.LogInformation(
                "Termination decision from LLM: {Decision}. Reason: {Reason}",
                responseTask.Value,
                responseTask.Reason ?? "No reason provided");

            return new GroupChatManagerResult<bool>(responseTask.Value)
            {
                Reason = "Determined by group chat manager prompt response."
            };
        }

        /// <summary>
        /// Sends a prompt to the LLM and parses the structured JSON response.
        /// </summary>
        /// <typeparam name="TValue">The type of value expected in the response.</typeparam>
        /// <param name="history">The chat history for context.</param>
        /// <param name="prompt">The prompt to send to the LLM.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A result containing the parsed response value.</returns>
        /// <exception cref="ChatManagerException">Thrown when the response cannot be parsed or is null.</exception>
        private async ValueTask<GroupChatManagerResult<TValue>> GetResponseAsync<TValue>(
            ChatHistory history,
            string prompt,
            CancellationToken cancellationToken = default)
        {
            try
            {
                OpenAIPromptExecutionSettings executionSettings = new()
                {
                    ResponseFormat = typeof(GroupChatManagerResult<TValue>)
                };

                ChatHistory request = [.. history, new ChatMessageContent(AuthorRole.System, prompt)];

                _logger.LogInformation("Sending request to LLM. History: {HistoryCount}, Prompt: {PromptLength}", 
                    history.Count, prompt.Length);

                var sw = Stopwatch.StartNew();
                var response = await _chatCompletion.GetChatMessageContentsAsync(
                    request,
                    executionSettings,
                    kernel: null,
                    cancellationToken);
                sw.Stop();

                _logger.LogInformation("LLM request received in {Elapsed}ms", sw.ElapsedMilliseconds);

                string responseText = response.FirstOrDefault()?.ToString() ?? string.Empty;

                if (string.IsNullOrWhiteSpace(responseText))
                {
                    _logger.LogError("Received empty response from LLM");
                    throw new ChatManagerException("LLM returned an empty response");
                }

                var result = JsonSerializer.Deserialize<GroupChatManagerResult<TValue>>(responseText);

                if (result == null)
                {
                    _logger.LogError("Deserialization returned null. Response length: {Length}", responseText.Length);
                    throw new ChatManagerException("Failed to deserialize LLM response to expected format");
                }

                _logger.LogInformation("Successfully parsed LLM response. Raw Response: {Value}", responseText);
                _logger.LogInformation("Deserialized Result: {Result}", result.Value);

                return result;
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "JSON parsing failed while deserializing LLM response");
                throw new ChatManagerException("Invalid JSON response from LLM", ex);
            }
            catch (Exception ex) 
            {
                _logger.LogError(ex, "Unexpected error while getting LLM response");
                throw new ChatManagerException("Error communicating with LLM", ex);
            }
        }

        #endregion
    }
#pragma warning restore SKEXP0110 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
}
