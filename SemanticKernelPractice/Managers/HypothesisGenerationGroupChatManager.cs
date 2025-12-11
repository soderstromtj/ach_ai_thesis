using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents.Orchestration.GroupChat;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using SemanticKernelPractice.Exceptions;
using SemanticKernelPractice.Models;
using System.Text.Json;

namespace SemanticKernelPractice.Managers
{
#pragma warning disable SKEXP0110 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
    /// <summary>
    /// Group chat manager for hypothesis generation workflows using the Analysis of Competing Hypotheses (ACH) framework.
    /// Orchestrates a team of agents to collaboratively generate hypotheses for a given key question.
    /// </summary>
    public class HypothesisGenerationGroupChatManager : GroupChatManager
    {
        private readonly OrchestrationPromptInput _input;
        private readonly List<string> _agentNames;
        private readonly IChatCompletionService _chatCompletion;
        private readonly IGroupChatPromptStrategy _promptStrategy;
        private readonly AgentParticipationTracker _participationTracker;
        private readonly ILogger<HypothesisGenerationGroupChatManager> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="HypothesisGenerationGroupChatManager"/> class.
        /// </summary>
        /// <param name="input">The orchestration prompt input containing the key question and context.</param>
        /// <param name="agentNames">The names of all agents participating in the group chat.</param>
        /// <param name="maximumInvocationLimit">The maximum number of turns allowed (0 or negative for unlimited).</param>
        /// <param name="chatCompletion">The chat completion service for LLM interactions.</param>
        /// <param name="promptStrategy">The strategy for generating prompts.</param>
        /// <param name="participationTracker">The tracker for monitoring agent participation.</param>
        /// <param name="logger">The logger for diagnostic information.</param>
        /// <exception cref="ArgumentNullException">Thrown when any required parameter is null.</exception>
        /// <exception cref="ArgumentException">Thrown when agentNames is empty.</exception>
        public HypothesisGenerationGroupChatManager(
            OrchestrationPromptInput input,
            List<string> agentNames,
            IChatCompletionService chatCompletion,
            IGroupChatPromptStrategy promptStrategy,
            AgentParticipationTracker participationTracker,
            ILogger<HypothesisGenerationGroupChatManager> logger)
        {
            ArgumentNullException.ThrowIfNull(input);
            ArgumentNullException.ThrowIfNull(agentNames);
            ArgumentNullException.ThrowIfNull(chatCompletion);
            ArgumentNullException.ThrowIfNull(promptStrategy);
            ArgumentNullException.ThrowIfNull(participationTracker);
            ArgumentNullException.ThrowIfNull(logger);

            if (agentNames.Count == 0)
            {
                throw new ArgumentException("At least one agent name is required", nameof(agentNames));
            }

            _input = input;
            _agentNames = agentNames;
            _chatCompletion = chatCompletion;
            _promptStrategy = promptStrategy;
            _participationTracker = participationTracker;
            _logger = logger;

            _logger.LogInformation(
                "HypothesisGenerationGroupChatManager created with {AgentCount} agents and max limit of {MaxLimit}",
                agentNames.Count,
                MaximumInvocationCount > 0 ? MaximumInvocationCount.ToString() : "unlimited");
        }

        /// <summary>
        /// Filters and formats the results from the group chat into a structured format.
        /// </summary>
        /// <param name="history">The chat history containing the conversation.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A result containing the filtered and formatted hypotheses.</returns>
        public override async ValueTask<GroupChatManagerResult<string>> FilterResults(
            ChatHistory history,
            CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Filtering results from chat history with {MessageCount} messages", history.Count);

            string prompt = _promptStrategy.GetFilterPrompt(_input);

            var response = await GetResponseAsync<string>(history, prompt, cancellationToken);

            return response;
        }

        /// <summary>
        /// Selects the next agent to contribute to the group chat.
        /// </summary>
        /// <param name="history">The chat history containing the conversation.</param>
        /// <param name="team">The group chat team.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A result containing the name of the selected agent.</returns>
        public override async ValueTask<GroupChatManagerResult<string>> SelectNextAgent(
            ChatHistory history,
            GroupChatTeam team,
            CancellationToken cancellationToken = default)
        {
            int turnCount = GetTurnCount(history);

            // If all of the DIME-FIL and deception agents must participate first, ensure they do so before considering others
            List<string> phaseOneAgents = new()
            {
                "DiplomaticHypothesisAgent",
                "InformationHypothesisAgent",
                "MilitaryHypothesisAgent",
                "EconomicHypothesisAgent",
                "FinancialHypothesisAgent",
                "IntelligenceHypothesisAgent",
                "LawEnforcementHypothesisAgent",
                "DeceptionHypothesisAgent"
            };

            string prompt;
            if (turnCount <= phaseOneAgents.Count)
            {
                _logger.LogDebug("Selecting next Phase 1 (brainstorming) agent.");
                prompt = _promptStrategy.GetSelectionPrompt(_input, phaseOneAgents);
                return await GetResponseAsync<string>(history, prompt, cancellationToken);
            }

            if (turnCount == phaseOneAgents.Count + 1)
            {
                _logger.LogDebug("Selecting Hypothesis Screening Agent after Phase 1 completion");
                prompt = _promptStrategy.GetSelectionPrompt(_input, _agentNames.Select(name => name == "HypothesisScreeningAgent" ? name : string.Empty).Where(name => !string.IsNullOrWhiteSpace(name)).ToList());
                return await GetResponseAsync<string>(history, prompt, cancellationToken);
            }

            
            _logger.LogDebug("Selecting Summarizing Agent after Hypothesis Screening completion");
            prompt = _promptStrategy.GetSelectionPrompt(_input, _agentNames.Select(name => name == "FinalHypothesisSummarizerFormatter" ? name : string.Empty).Where(name => !string.IsNullOrWhiteSpace(name)).ToList());
            return await GetResponseAsync<string>(history, prompt, cancellationToken);
            
        }

        /// <summary>
        /// Determines whether user input should be requested.
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
        /// Determines whether the group chat should terminate based on various criteria.
        /// </summary>
        /// <param name="history">The chat history containing the conversation.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A result indicating whether the chat should terminate and the reason.</returns>
        public override async ValueTask<GroupChatManagerResult<bool>> ShouldTerminate(
            ChatHistory history,
            CancellationToken cancellationToken = default)
        {
            int turnCount = GetTurnCount(history);

            _logger.LogDebug(
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

            // Delegate to LLM for quality assessment
            return await EvaluateTerminationCriteria(history, cancellationToken);
        }

        #region Private Methods

        /// <summary>
        /// Gets the current turn count from the chat history.
        /// </summary>
        /// <param name="history">The chat history to analyze.</param>
        /// <returns>The number of assistant messages in the history.</returns>
        private int GetTurnCount(ChatHistory history)
        {
            return history.Count(msg => msg.Role == AuthorRole.Assistant);
        }

        /// <summary>
        /// Checks if the maximum invocation limit has been reached.
        /// </summary>
        /// <param name="turnCount">The current turn count.</param>
        /// <returns>True if the limit has been reached; otherwise, false.</returns>
        private bool HasReachedMaximumLimit(int turnCount)
        {
            return MaximumInvocationCount > 0 && turnCount >= MaximumInvocationCount;
        }

        /// <summary>
        /// Checks if all expected agents have participated in the conversation.
        /// </summary>
        /// <param name="history">The chat history to analyze.</param>
        /// <returns>True if all agents have participated; otherwise, false.</returns>
        private bool HaveAllAgentsParticipated(ChatHistory history)
        {
            return _participationTracker.HaveAllAgentsParticipated(history, _agentNames);
        }

        /// <summary>
        /// Evaluates termination criteria by delegating to the LLM for quality assessment.
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
        /// Gets a response from the chat completion service using structured output.
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

                _logger.LogTrace("Sending request to LLM with {HistoryCount} history messages", history.Count);

                var response = await _chatCompletion.GetChatMessageContentsAsync(
                    request,
                    executionSettings,
                    kernel: null,
                    cancellationToken);

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

                _logger.LogTrace("Successfully parsed LLM response to {Type}", typeof(TValue).Name);

                return result;
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "JSON parsing failed while deserializing LLM response");
                throw new ChatManagerException("Invalid JSON response from LLM", ex);
            }
            catch (Exception ex) when (ex is not ChatManagerException)
            {
                _logger.LogError(ex, "Unexpected error while getting LLM response");
                throw new ChatManagerException("Error communicating with LLM", ex);
            }
        }

        #endregion
    }
#pragma warning restore SKEXP0110 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
}
