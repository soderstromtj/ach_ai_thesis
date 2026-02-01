using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents.Orchestration.GroupChat;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using NIU.ACH_AI.Application.DTOs;
using NIU.ACH_AI.Application.Exceptions;
using NIU.ACH_AI.Application.Interfaces;
using System.Diagnostics;
using System.Text.Json;

namespace NIU.ACH_AI.Infrastructure.AI.Managers
{
#pragma warning disable SKEXP0110 // Type is for evaluation purposes only
    /// <summary>
    /// Group chat manager for evidence extraction workflows.
    /// Orchestrates a team of agents to collaboratively extract, review, and deduplicate evidence.
    /// </summary>
    public class EvidenceExtractionGroupChatManager : GroupChatManager
    {
        private readonly OrchestrationPromptInput _input;
        private readonly IEnumerable<string> _agentNames;
        private readonly IChatCompletionService _chatCompletion;
        private readonly IGroupChatPromptStrategy _promptStrategy;
        private readonly AgentParticipationTracker _participationTracker;
        private readonly ILogger<EvidenceExtractionGroupChatManager> _logger;

        public EvidenceExtractionGroupChatManager(
            OrchestrationPromptInput input,
            IEnumerable<string> agentNames,
            IChatCompletionService chatCompletion,
            IGroupChatPromptStrategy promptStrategy,
            AgentParticipationTracker participationTracker,
            ILogger<EvidenceExtractionGroupChatManager> logger)
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
                "EvidenceExtractionGroupChatManager created with {AgentCount} agents and max limit of {MaxLimit}",
                _agentNames.Count(),
                MaximumInvocationCount > 0 ? MaximumInvocationCount.ToString() : "unlimited");
        }

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

        public override ValueTask<GroupChatManagerResult<bool>> ShouldRequestUserInput(
            ChatHistory history,
            CancellationToken cancellationToken = default)
        {
            _logger.LogTrace("User input not required for automated evidence extraction");

            return ValueTask.FromResult(
                new GroupChatManagerResult<bool>(false)
                {
                    Reason = "Automated evidence extraction workflow - no user input needed."
                });
        }

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

        private int GetTurnCount(ChatHistory history)
        {
            return history.Count(msg => msg.Role == AuthorRole.Assistant);
        }

        private bool HasReachedMaximumLimit(int turnCount)
        {
            return MaximumInvocationCount > 0 && turnCount >= MaximumInvocationCount;
        }

        private bool HaveAllAgentsParticipated(ChatHistory history)
        {
            return _participationTracker.HaveAllAgentsParticipated(history, _agentNames);
        }

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

                // Temporary fix for JSON markdown blocks sometimes returned by models
                responseText = responseText.Replace("```json", "").Replace("```", "").Trim();

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
#pragma warning restore SKEXP0110
}
