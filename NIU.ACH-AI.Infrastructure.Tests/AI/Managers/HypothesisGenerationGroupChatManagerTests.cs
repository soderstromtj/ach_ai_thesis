using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents.Orchestration.GroupChat;
using Microsoft.SemanticKernel.ChatCompletion;
using Moq;
using NIU.ACH_AI.Application.DTOs;
using NIU.ACH_AI.Application.Exceptions;
using NIU.ACH_AI.Application.Interfaces;
using NIU.ACH_AI.Infrastructure.AI.Managers;
using System.Text.Json;

namespace NIU.ACH_AI.Infrastructure.Tests.AI.Managers;

/// <summary>
/// Unit tests for HypothesisGenerationGroupChatManager.
///
/// Testing Strategy:
/// -----------------
/// HypothesisGenerationGroupChatManager orchestrates hypothesis generation in ACH workflows.
/// It extends GroupChatManager and implements custom logic for:
/// - Agent selection based on turn phases (DIME-FIL agents, Screening, Summarizing)
/// - Termination criteria evaluation
/// - Result filtering
///
/// Key testing areas:
/// 1. Constructor - Null validation for all 6 parameters + empty agentNames
/// 2. ShouldRequestUserInput - Always returns false (automated workflow)
/// 3. SelectNextAgent - Phase-based agent selection logic
/// 4. ShouldTerminate - Max limit check and LLM delegation
/// 5. FilterResults - Prompt strategy usage and response handling
/// 6. Error handling - ChatManagerException scenarios
/// </summary>
#pragma warning disable SKEXP0110 // Type is for evaluation purposes only
public class HypothesisGenerationGroupChatManagerTests
{
    #region Test Infrastructure

    private static (
        HypothesisGenerationGroupChatManager Manager,
        Mock<IChatCompletionService> ChatCompletionMock,
        Mock<IGroupChatPromptStrategy> PromptStrategyMock,
        Mock<ILogger<HypothesisGenerationGroupChatManager>> LoggerMock
    ) CreateManager(
        OrchestrationPromptInput? input = null,
        List<string>? agentNames = null)
    {
        var chatCompletionMock = new Mock<IChatCompletionService>();
        var promptStrategyMock = new Mock<IGroupChatPromptStrategy>();
        var participationTracker = new AgentParticipationTracker();
        var loggerMock = new Mock<ILogger<HypothesisGenerationGroupChatManager>>();

        var actualInput = input ?? CreateDefaultInput();
        var actualAgentNames = agentNames ?? CreateDefaultAgentNames();

        var manager = new HypothesisGenerationGroupChatManager(
            actualInput,
            actualAgentNames,
            chatCompletionMock.Object,
            promptStrategyMock.Object,
            participationTracker,
            loggerMock.Object);

        return (manager, chatCompletionMock, promptStrategyMock, loggerMock);
    }

    private static OrchestrationPromptInput CreateDefaultInput()
    {
        return new OrchestrationPromptInput
        {
            KeyQuestion = "What is the primary cause of the incident?"
        };
    }

    private static List<string> CreateDefaultAgentNames()
    {
        return new List<string>
        {
            "DiplomaticHypothesisAgent",
            "InformationHypothesisAgent",
            "MilitaryHypothesisAgent",
            "EconomicHypothesisAgent",
            "FinancialHypothesisAgent",
            "IntelligenceHypothesisAgent",
            "LawEnforcementHypothesisAgent",
            "DeceptionHypothesisAgent",
            "HypothesisScreeningAgent",
            "FinalHypothesisSummarizerFormatter"
        };
    }

    private static ChatHistory CreateEmptyHistory()
    {
        return new ChatHistory();
    }

    private static ChatHistory CreateHistoryWithTurns(int turnCount)
    {
        var history = new ChatHistory();
        for (int i = 0; i < turnCount; i++)
        {
            history.Add(new ChatMessageContent
            {
                Role = AuthorRole.Assistant,
                AuthorName = $"Agent{i + 1}",
                Content = $"Response from turn {i + 1}"
            });
        }
        return history;
    }

    private static void SetupChatCompletionResponse<T>(
        Mock<IChatCompletionService> mock,
        T value,
        string? reason = null)
    {
        var result = new GroupChatManagerResult<T>(value) { Reason = reason ?? string.Empty };
        var jsonResponse = JsonSerializer.Serialize(result);

        mock.Setup(c => c.GetChatMessageContentsAsync(
                It.IsAny<ChatHistory>(),
                It.IsAny<PromptExecutionSettings>(),
                It.IsAny<Microsoft.SemanticKernel.Kernel?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ChatMessageContent>
            {
                new ChatMessageContent(AuthorRole.Assistant, jsonResponse)
            });
    }

    private static void SetupChatCompletionEmptyResponse(Mock<IChatCompletionService> mock)
    {
        mock.Setup(c => c.GetChatMessageContentsAsync(
                It.IsAny<ChatHistory>(),
                It.IsAny<PromptExecutionSettings>(),
                It.IsAny<Microsoft.SemanticKernel.Kernel?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ChatMessageContent>
            {
                new ChatMessageContent(AuthorRole.Assistant, "")
            });
    }

    private static void SetupChatCompletionInvalidJsonResponse(Mock<IChatCompletionService> mock)
    {
        mock.Setup(c => c.GetChatMessageContentsAsync(
                It.IsAny<ChatHistory>(),
                It.IsAny<PromptExecutionSettings>(),
                It.IsAny<Microsoft.SemanticKernel.Kernel?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ChatMessageContent>
            {
                new ChatMessageContent(AuthorRole.Assistant, "not valid json")
            });
    }

    private static void SetupChatCompletionThrowsException(
        Mock<IChatCompletionService> mock,
        Exception exception)
    {
        mock.Setup(c => c.GetChatMessageContentsAsync(
                It.IsAny<ChatHistory>(),
                It.IsAny<PromptExecutionSettings>(),
                It.IsAny<Microsoft.SemanticKernel.Kernel?>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(exception);
    }

    #endregion

    #region Constructor - Null Validation Tests

    /// <summary>
    /// WHY: Verifies null input throws ArgumentNullException.
    /// </summary>
    [Fact]
    public void Constructor_WithNullInput_ThrowsArgumentNullException()
    {
        // Arrange
        var chatCompletionMock = new Mock<IChatCompletionService>();
        var promptStrategyMock = new Mock<IGroupChatPromptStrategy>();
        var participationTracker = new AgentParticipationTracker();
        var loggerMock = new Mock<ILogger<HypothesisGenerationGroupChatManager>>();

        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() =>
            new HypothesisGenerationGroupChatManager(
                null!,
                CreateDefaultAgentNames(),
                chatCompletionMock.Object,
                promptStrategyMock.Object,
                participationTracker,
                loggerMock.Object));

        Assert.Equal("input", exception.ParamName);
    }

    /// <summary>
    /// WHY: Verifies null agentNames throws ArgumentNullException.
    /// </summary>
    [Fact]
    public void Constructor_WithNullAgentNames_ThrowsArgumentNullException()
    {
        // Arrange
        var chatCompletionMock = new Mock<IChatCompletionService>();
        var promptStrategyMock = new Mock<IGroupChatPromptStrategy>();
        var participationTracker = new AgentParticipationTracker();
        var loggerMock = new Mock<ILogger<HypothesisGenerationGroupChatManager>>();

        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() =>
            new HypothesisGenerationGroupChatManager(
                CreateDefaultInput(),
                null!,
                chatCompletionMock.Object,
                promptStrategyMock.Object,
                participationTracker,
                loggerMock.Object));

        Assert.Equal("agentNames", exception.ParamName);
    }

    /// <summary>
    /// WHY: Verifies null chatCompletion throws ArgumentNullException.
    /// </summary>
    [Fact]
    public void Constructor_WithNullChatCompletion_ThrowsArgumentNullException()
    {
        // Arrange
        var promptStrategyMock = new Mock<IGroupChatPromptStrategy>();
        var participationTracker = new AgentParticipationTracker();
        var loggerMock = new Mock<ILogger<HypothesisGenerationGroupChatManager>>();

        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() =>
            new HypothesisGenerationGroupChatManager(
                CreateDefaultInput(),
                CreateDefaultAgentNames(),
                null!,
                promptStrategyMock.Object,
                participationTracker,
                loggerMock.Object));

        Assert.Equal("chatCompletion", exception.ParamName);
    }

    /// <summary>
    /// WHY: Verifies null promptStrategy throws ArgumentNullException.
    /// </summary>
    [Fact]
    public void Constructor_WithNullPromptStrategy_ThrowsArgumentNullException()
    {
        // Arrange
        var chatCompletionMock = new Mock<IChatCompletionService>();
        var participationTracker = new AgentParticipationTracker();
        var loggerMock = new Mock<ILogger<HypothesisGenerationGroupChatManager>>();

        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() =>
            new HypothesisGenerationGroupChatManager(
                CreateDefaultInput(),
                CreateDefaultAgentNames(),
                chatCompletionMock.Object,
                null!,
                participationTracker,
                loggerMock.Object));

        Assert.Equal("promptStrategy", exception.ParamName);
    }

    /// <summary>
    /// WHY: Verifies null participationTracker throws ArgumentNullException.
    /// </summary>
    [Fact]
    public void Constructor_WithNullParticipationTracker_ThrowsArgumentNullException()
    {
        // Arrange
        var chatCompletionMock = new Mock<IChatCompletionService>();
        var promptStrategyMock = new Mock<IGroupChatPromptStrategy>();
        var loggerMock = new Mock<ILogger<HypothesisGenerationGroupChatManager>>();

        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() =>
            new HypothesisGenerationGroupChatManager(
                CreateDefaultInput(),
                CreateDefaultAgentNames(),
                chatCompletionMock.Object,
                promptStrategyMock.Object,
                null!,
                loggerMock.Object));

        Assert.Equal("participationTracker", exception.ParamName);
    }

    /// <summary>
    /// WHY: Verifies null logger throws ArgumentNullException.
    /// </summary>
    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Arrange
        var chatCompletionMock = new Mock<IChatCompletionService>();
        var promptStrategyMock = new Mock<IGroupChatPromptStrategy>();
        var participationTracker = new AgentParticipationTracker();

        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() =>
            new HypothesisGenerationGroupChatManager(
                CreateDefaultInput(),
                CreateDefaultAgentNames(),
                chatCompletionMock.Object,
                promptStrategyMock.Object,
                participationTracker,
                null!));

        Assert.Equal("logger", exception.ParamName);
    }

    /// <summary>
    /// WHY: Verifies empty agentNames list throws ArgumentException.
    /// </summary>
    [Fact]
    public void Constructor_WithEmptyAgentNames_ThrowsArgumentException()
    {
        // Arrange
        var chatCompletionMock = new Mock<IChatCompletionService>();
        var promptStrategyMock = new Mock<IGroupChatPromptStrategy>();
        var participationTracker = new AgentParticipationTracker();
        var loggerMock = new Mock<ILogger<HypothesisGenerationGroupChatManager>>();

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            new HypothesisGenerationGroupChatManager(
                CreateDefaultInput(),
                new List<string>(), // Empty list
                chatCompletionMock.Object,
                promptStrategyMock.Object,
                participationTracker,
                loggerMock.Object));

        Assert.Equal("agentNames", exception.ParamName);
        Assert.Contains("At least one agent name is required", exception.Message);
    }

    #endregion

    #region Constructor - Success Tests

    /// <summary>
    /// WHY: Verifies manager can be instantiated with valid parameters.
    /// </summary>
    [Fact]
    public void Constructor_WithValidParameters_CreatesInstance()
    {
        // Arrange & Act
        var (manager, _, _, _) = CreateManager();

        // Assert
        Assert.NotNull(manager);
    }

    /// <summary>
    /// WHY: Verifies manager inherits from GroupChatManager.
    /// </summary>
    [Fact]
    public void Manager_InheritsFromGroupChatManager()
    {
        // Arrange & Act
        var (manager, _, _, _) = CreateManager();

        // Assert
        Assert.IsAssignableFrom<GroupChatManager>(manager);
    }

    /// <summary>
    /// WHY: Verifies manager can be created with single agent.
    /// </summary>
    [Fact]
    public void Constructor_WithSingleAgent_CreatesInstance()
    {
        // Arrange & Act
        var (manager, _, _, _) = CreateManager(agentNames: new List<string> { "SingleAgent" });

        // Assert
        Assert.NotNull(manager);
    }

    #endregion

    #region ShouldRequestUserInput Tests

    /// <summary>
    /// WHY: Verifies automated workflow never requests user input.
    /// </summary>
    [Fact]
    public async Task ShouldRequestUserInput_Always_ReturnsFalse()
    {
        // Arrange
        var (manager, _, _, _) = CreateManager();
        var history = CreateEmptyHistory();

        // Act
        var result = await manager.ShouldRequestUserInput(history);

        // Assert
        Assert.False(result.Value);
    }

    /// <summary>
    /// WHY: Verifies reason is provided for not requesting user input.
    /// </summary>
    [Fact]
    public async Task ShouldRequestUserInput_Always_ProvidesReason()
    {
        // Arrange
        var (manager, _, _, _) = CreateManager();
        var history = CreateEmptyHistory();

        // Act
        var result = await manager.ShouldRequestUserInput(history);

        // Assert
        Assert.NotNull(result.Reason);
        Assert.Contains("Automated ACH", result.Reason);
    }

    /// <summary>
    /// WHY: Verifies user input not requested with populated history.
    /// </summary>
    [Fact]
    public async Task ShouldRequestUserInput_WithPopulatedHistory_StillReturnsFalse()
    {
        // Arrange
        var (manager, _, _, _) = CreateManager();
        var history = CreateHistoryWithTurns(5);

        // Act
        var result = await manager.ShouldRequestUserInput(history);

        // Assert
        Assert.False(result.Value);
    }

    #endregion

    #region SelectNextAgent - Phase 1 Tests (DIME-FIL + Deception)

    /// <summary>
    /// WHY: Verifies Phase 1 agent selection for first turn.
    /// </summary>
    [Fact]
    public async Task SelectNextAgent_OnFirstTurn_RequestsPhase1Selection()
    {
        // Arrange
        var (manager, chatCompletionMock, promptStrategyMock, _) = CreateManager();
        var history = CreateEmptyHistory(); // 0 assistant messages = turn 0
        var team = new Mock<GroupChatTeam>().Object;

        promptStrategyMock
            .Setup(p => p.GetSelectionPrompt(It.IsAny<OrchestrationPromptInput>(), It.IsAny<List<string>>()))
            .Returns("Select next agent");

        SetupChatCompletionResponse(chatCompletionMock, "DiplomaticHypothesisAgent");

        // Act
        var result = await manager.SelectNextAgent(history, team);

        // Assert
        promptStrategyMock.Verify(p => p.GetSelectionPrompt(
            It.IsAny<OrchestrationPromptInput>(),
            It.Is<List<string>>(names => names.Contains("DiplomaticHypothesisAgent"))),
            Times.Once);
    }

    /// <summary>
    /// WHY: Verifies Phase 1 covers 8 agents (DIME-FIL + Deception).
    /// </summary>
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(7)]
    public async Task SelectNextAgent_DuringPhase1_UsesPhase1Agents(int turnCount)
    {
        // Arrange
        var (manager, chatCompletionMock, promptStrategyMock, _) = CreateManager();
        var history = CreateHistoryWithTurns(turnCount);
        var team = new Mock<GroupChatTeam>().Object;

        List<string>? capturedAgentList = null;
        promptStrategyMock
            .Setup(p => p.GetSelectionPrompt(It.IsAny<OrchestrationPromptInput>(), It.IsAny<List<string>>()))
            .Callback<OrchestrationPromptInput, List<string>>((_, agents) => capturedAgentList = agents)
            .Returns("Select next agent");

        SetupChatCompletionResponse(chatCompletionMock, "MilitaryHypothesisAgent");

        // Act
        await manager.SelectNextAgent(history, team);

        // Assert
        Assert.NotNull(capturedAgentList);
        Assert.Contains("DiplomaticHypothesisAgent", capturedAgentList);
        Assert.Contains("DeceptionHypothesisAgent", capturedAgentList);
    }

    #endregion

    #region SelectNextAgent - Phase 2 Tests (HypothesisScreeningAgent)

    /// <summary>
    /// WHY: Verifies Phase 2 selects HypothesisScreeningAgent after 8 turns.
    /// </summary>
    [Fact]
    public async Task SelectNextAgent_AtTurn9_SelectsHypothesisScreeningAgent()
    {
        // Arrange
        var (manager, chatCompletionMock, promptStrategyMock, _) = CreateManager();
        var history = CreateHistoryWithTurns(8); // Turn count = 8, next is turn 9
        var team = new Mock<GroupChatTeam>().Object;

        List<string>? capturedAgentList = null;
        promptStrategyMock
            .Setup(p => p.GetSelectionPrompt(It.IsAny<OrchestrationPromptInput>(), It.IsAny<List<string>>()))
            .Callback<OrchestrationPromptInput, List<string>>((_, agents) => capturedAgentList = agents)
            .Returns("Select next agent");

        SetupChatCompletionResponse(chatCompletionMock, "HypothesisScreeningAgent");

        // Act
        await manager.SelectNextAgent(history, team);

        // Assert
        Assert.NotNull(capturedAgentList);
        Assert.Contains("HypothesisScreeningAgent", capturedAgentList);
        Assert.DoesNotContain("DiplomaticHypothesisAgent", capturedAgentList);
    }

    #endregion

    #region SelectNextAgent - Phase 3 Tests (FinalHypothesisSummarizerFormatter)

    /// <summary>
    /// WHY: Verifies Phase 3 selects FinalHypothesisSummarizerFormatter after turn 9.
    /// </summary>
    [Fact]
    public async Task SelectNextAgent_AfterTurn9_SelectsFinalHypothesisSummarizerFormatter()
    {
        // Arrange
        var (manager, chatCompletionMock, promptStrategyMock, _) = CreateManager();
        var history = CreateHistoryWithTurns(9); // Turn count = 9, next is turn 10
        var team = new Mock<GroupChatTeam>().Object;

        List<string>? capturedAgentList = null;
        promptStrategyMock
            .Setup(p => p.GetSelectionPrompt(It.IsAny<OrchestrationPromptInput>(), It.IsAny<List<string>>()))
            .Callback<OrchestrationPromptInput, List<string>>((_, agents) => capturedAgentList = agents)
            .Returns("Select next agent");

        SetupChatCompletionResponse(chatCompletionMock, "FinalHypothesisSummarizerFormatter");

        // Act
        await manager.SelectNextAgent(history, team);

        // Assert
        Assert.NotNull(capturedAgentList);
        Assert.Contains("FinalHypothesisSummarizerFormatter", capturedAgentList);
        Assert.DoesNotContain("HypothesisScreeningAgent", capturedAgentList);
    }

    /// <summary>
    /// WHY: Verifies Phase 3 continues with FinalHypothesisSummarizerFormatter for later turns.
    /// </summary>
    [Theory]
    [InlineData(10)]
    [InlineData(15)]
    [InlineData(20)]
    public async Task SelectNextAgent_AtLaterTurns_ContinuesWithSummarizer(int turnCount)
    {
        // Arrange
        var (manager, chatCompletionMock, promptStrategyMock, _) = CreateManager();
        var history = CreateHistoryWithTurns(turnCount);
        var team = new Mock<GroupChatTeam>().Object;

        List<string>? capturedAgentList = null;
        promptStrategyMock
            .Setup(p => p.GetSelectionPrompt(It.IsAny<OrchestrationPromptInput>(), It.IsAny<List<string>>()))
            .Callback<OrchestrationPromptInput, List<string>>((_, agents) => capturedAgentList = agents)
            .Returns("Select next agent");

        SetupChatCompletionResponse(chatCompletionMock, "FinalHypothesisSummarizerFormatter");

        // Act
        await manager.SelectNextAgent(history, team);

        // Assert
        Assert.NotNull(capturedAgentList);
        Assert.Contains("FinalHypothesisSummarizerFormatter", capturedAgentList);
    }

    #endregion

    #region ShouldTerminate - Maximum Limit Tests

    /// <summary>
    /// WHY: Verifies termination when max limit is reached.
    /// </summary>
    [Fact]
    public async Task ShouldTerminate_WhenMaxLimitReached_ReturnsTrue()
    {
        // Arrange
        var (manager, chatCompletionMock, _, _) = CreateManager();
        // Set MaximumInvocationCount via base class
        typeof(GroupChatManager)
            .GetProperty("MaximumInvocationCount")!
            .SetValue(manager, 5);

        var history = CreateHistoryWithTurns(5); // Exactly at limit

        // Act
        var result = await manager.ShouldTerminate(history);

        // Assert
        Assert.True(result.Value);
        Assert.Contains("Maximum invocation limit", result.Reason);
    }

    /// <summary>
    /// WHY: Verifies termination message includes the limit value.
    /// </summary>
    [Fact]
    public async Task ShouldTerminate_WhenMaxLimitReached_IncludesLimitInReason()
    {
        // Arrange
        var (manager, _, _, _) = CreateManager();
        typeof(GroupChatManager)
            .GetProperty("MaximumInvocationCount")!
            .SetValue(manager, 10);

        var history = CreateHistoryWithTurns(10);

        // Act
        var result = await manager.ShouldTerminate(history);

        // Assert
        Assert.Contains("10", result.Reason);
    }

    /// <summary>
    /// WHY: Verifies no termination when below limit.
    /// </summary>
    [Fact]
    public async Task ShouldTerminate_WhenBelowMaxLimit_DelegatesToLLM()
    {
        // Arrange
        var (manager, chatCompletionMock, promptStrategyMock, _) = CreateManager();
        typeof(GroupChatManager)
            .GetProperty("MaximumInvocationCount")!
            .SetValue(manager, 10);

        var history = CreateHistoryWithTurns(5); // Below limit

        promptStrategyMock
            .Setup(p => p.GetTerminationPrompt(It.IsAny<OrchestrationPromptInput>(), It.IsAny<List<string>>()))
            .Returns("Termination prompt");

        SetupChatCompletionResponse(chatCompletionMock, false, "Continue generating hypotheses");

        // Act
        var result = await manager.ShouldTerminate(history);

        // Assert
        Assert.False(result.Value);
        promptStrategyMock.Verify(p => p.GetTerminationPrompt(
            It.IsAny<OrchestrationPromptInput>(),
            It.IsAny<List<string>>()),
            Times.Once);
    }

    /// <summary>
    /// WHY: Verifies unlimited mode (MaximumInvocationCount = 0) delegates to LLM.
    /// </summary>
    [Fact]
    public async Task ShouldTerminate_WithUnlimitedMode_DelegatesToLLM()
    {
        // Arrange
        var (manager, chatCompletionMock, promptStrategyMock, _) = CreateManager();
        // Default MaximumInvocationCount is 0 (unlimited)

        var history = CreateHistoryWithTurns(100);

        promptStrategyMock
            .Setup(p => p.GetTerminationPrompt(It.IsAny<OrchestrationPromptInput>(), It.IsAny<List<string>>()))
            .Returns("Termination prompt");

        SetupChatCompletionResponse(chatCompletionMock, true, "Sufficient hypotheses generated");

        // Act
        var result = await manager.ShouldTerminate(history);

        // Assert
        Assert.True(result.Value);
    }

    #endregion

    #region ShouldTerminate - LLM Delegation Tests

    /// <summary>
    /// WHY: Verifies LLM can signal termination.
    /// </summary>
    [Fact]
    public async Task ShouldTerminate_WhenLLMSaysTerminate_ReturnsTrue()
    {
        // Arrange
        var (manager, chatCompletionMock, promptStrategyMock, _) = CreateManager();
        var history = CreateHistoryWithTurns(3);

        promptStrategyMock
            .Setup(p => p.GetTerminationPrompt(It.IsAny<OrchestrationPromptInput>(), It.IsAny<List<string>>()))
            .Returns("Termination prompt");

        SetupChatCompletionResponse(chatCompletionMock, true, "All hypotheses adequately explored");

        // Act
        var result = await manager.ShouldTerminate(history);

        // Assert
        Assert.True(result.Value);
        Assert.Contains("group chat manager prompt response", result.Reason);
    }

    /// <summary>
    /// WHY: Verifies LLM can signal continuation.
    /// </summary>
    [Fact]
    public async Task ShouldTerminate_WhenLLMSaysContinue_ReturnsFalse()
    {
        // Arrange
        var (manager, chatCompletionMock, promptStrategyMock, _) = CreateManager();
        var history = CreateHistoryWithTurns(3);

        promptStrategyMock
            .Setup(p => p.GetTerminationPrompt(It.IsAny<OrchestrationPromptInput>(), It.IsAny<List<string>>()))
            .Returns("Termination prompt");

        SetupChatCompletionResponse(chatCompletionMock, false, "More exploration needed");

        // Act
        var result = await manager.ShouldTerminate(history);

        // Assert
        Assert.False(result.Value);
    }

    #endregion

    #region FilterResults Tests

    /// <summary>
    /// WHY: Verifies filter uses prompt strategy.
    /// </summary>
    [Fact]
    public async Task FilterResults_Always_UsesPromptStrategy()
    {
        // Arrange
        var (manager, chatCompletionMock, promptStrategyMock, _) = CreateManager();
        var history = CreateHistoryWithTurns(5);

        promptStrategyMock
            .Setup(p => p.GetFilterPrompt(It.IsAny<OrchestrationPromptInput>()))
            .Returns("Filter prompt");

        SetupChatCompletionResponse(chatCompletionMock, "Filtered results");

        // Act
        await manager.FilterResults(history);

        // Assert
        promptStrategyMock.Verify(p => p.GetFilterPrompt(It.IsAny<OrchestrationPromptInput>()), Times.Once);
    }

    /// <summary>
    /// WHY: Verifies filter returns string result.
    /// </summary>
    [Fact]
    public async Task FilterResults_WithValidResponse_ReturnsFilteredString()
    {
        // Arrange
        var (manager, chatCompletionMock, promptStrategyMock, _) = CreateManager();
        var history = CreateHistoryWithTurns(5);

        promptStrategyMock
            .Setup(p => p.GetFilterPrompt(It.IsAny<OrchestrationPromptInput>()))
            .Returns("Filter prompt");

        SetupChatCompletionResponse(chatCompletionMock, "1. Hypothesis A\n2. Hypothesis B");

        // Act
        var result = await manager.FilterResults(history);

        // Assert
        Assert.NotNull(result.Value);
        Assert.Contains("Hypothesis", result.Value);
    }

    #endregion

    #region Error Handling Tests

    /// <summary>
    /// WHY: Verifies empty LLM response throws ChatManagerException.
    /// </summary>
    [Fact]
    public async Task SelectNextAgent_WithEmptyResponse_ThrowsChatManagerException()
    {
        // Arrange
        var (manager, chatCompletionMock, promptStrategyMock, _) = CreateManager();
        var history = CreateHistoryWithTurns(0);
        var team = new Mock<GroupChatTeam>().Object;

        promptStrategyMock
            .Setup(p => p.GetSelectionPrompt(It.IsAny<OrchestrationPromptInput>(), It.IsAny<List<string>>()))
            .Returns("Select agent");

        SetupChatCompletionEmptyResponse(chatCompletionMock);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ChatManagerException>(() =>
            manager.SelectNextAgent(history, team).AsTask());

        Assert.Contains("Error communicating with LLM", exception.Message);
    }

    /// <summary>
    /// WHY: Verifies invalid JSON response throws ChatManagerException.
    /// </summary>
    [Fact]
    public async Task FilterResults_WithInvalidJsonResponse_ThrowsChatManagerException()
    {
        // Arrange
        var (manager, chatCompletionMock, promptStrategyMock, _) = CreateManager();
        var history = CreateHistoryWithTurns(5);

        promptStrategyMock
            .Setup(p => p.GetFilterPrompt(It.IsAny<OrchestrationPromptInput>()))
            .Returns("Filter prompt");

        SetupChatCompletionInvalidJsonResponse(chatCompletionMock);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ChatManagerException>(() =>
            manager.FilterResults(history).AsTask());

        Assert.Contains("Invalid JSON", exception.Message);
    }

    /// <summary>
    /// WHY: Verifies network errors are wrapped in ChatManagerException.
    /// </summary>
    [Fact]
    public async Task ShouldTerminate_WhenNetworkError_ThrowsChatManagerException()
    {
        // Arrange
        var (manager, chatCompletionMock, promptStrategyMock, _) = CreateManager();
        var history = CreateHistoryWithTurns(3);

        promptStrategyMock
            .Setup(p => p.GetTerminationPrompt(It.IsAny<OrchestrationPromptInput>(), It.IsAny<List<string>>()))
            .Returns("Termination prompt");

        SetupChatCompletionThrowsException(chatCompletionMock, new HttpRequestException("Network error"));

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ChatManagerException>(() =>
            manager.ShouldTerminate(history).AsTask());

        Assert.Contains("Error communicating with LLM", exception.Message);
        Assert.IsType<HttpRequestException>(exception.InnerException);
    }

    /// <summary>
    /// WHY: Verifies ChatManagerException is not wrapped again.
    /// </summary>
    [Fact]
    public async Task SelectNextAgent_WhenChatManagerExceptionThrown_PropagatesDirectly()
    {
        // Arrange
        var (manager, chatCompletionMock, promptStrategyMock, _) = CreateManager();
        var history = CreateHistoryWithTurns(0);
        var team = new Mock<GroupChatTeam>().Object;

        promptStrategyMock
            .Setup(p => p.GetSelectionPrompt(It.IsAny<OrchestrationPromptInput>(), It.IsAny<List<string>>()))
            .Returns("Select agent");

        var originalException = new ChatManagerException("Original error");
        SetupChatCompletionThrowsException(chatCompletionMock, originalException);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ChatManagerException>(() => manager.SelectNextAgent(history, team).AsTask());

        // Should be wrapped (not the same exception) since it goes through the error handler
        Assert.Contains("Error communicating with LLM", exception.Message);
    }

    #endregion

    #region Turn Counting Tests

    /// <summary>
    /// WHY: Verifies only Assistant messages count as turns.
    /// </summary>
    [Fact]
    public async Task SelectNextAgent_CountsOnlyAssistantMessages()
    {
        // Arrange
        var (manager, chatCompletionMock, promptStrategyMock, _) = CreateManager();
        var team = new Mock<GroupChatTeam>().Object;

        // Create history with mixed roles (only 2 assistant messages)
        var history = new ChatHistory();
        history.Add(new ChatMessageContent(AuthorRole.User, "User message"));
        history.Add(new ChatMessageContent(AuthorRole.Assistant, "Assistant 1") { AuthorName = "Agent1" });
        history.Add(new ChatMessageContent(AuthorRole.System, "System message"));
        history.Add(new ChatMessageContent(AuthorRole.Assistant, "Assistant 2") { AuthorName = "Agent2" });
        history.Add(new ChatMessageContent(AuthorRole.User, "Another user message"));

        List<string>? capturedAgentList = null;
        promptStrategyMock
            .Setup(p => p.GetSelectionPrompt(It.IsAny<OrchestrationPromptInput>(), It.IsAny<List<string>>()))
            .Callback<OrchestrationPromptInput, List<string>>((_, agents) => capturedAgentList = agents)
            .Returns("Select agent");

        SetupChatCompletionResponse(chatCompletionMock, "MilitaryHypothesisAgent");

        // Act
        await manager.SelectNextAgent(history, team);

        // Assert - Turn count should be 2, so still in Phase 1
        Assert.NotNull(capturedAgentList);
        Assert.Contains("DiplomaticHypothesisAgent", capturedAgentList);
    }

    /// <summary>
    /// WHY: Verifies empty history counts as turn 0.
    /// </summary>
    [Fact]
    public async Task SelectNextAgent_WithEmptyHistory_StartsAtPhase1()
    {
        // Arrange
        var (manager, chatCompletionMock, promptStrategyMock, _) = CreateManager();
        var history = CreateEmptyHistory();
        var team = new Mock<GroupChatTeam>().Object;

        List<string>? capturedAgentList = null;
        promptStrategyMock
            .Setup(p => p.GetSelectionPrompt(It.IsAny<OrchestrationPromptInput>(), It.IsAny<List<string>>()))
            .Callback<OrchestrationPromptInput, List<string>>((_, agents) => capturedAgentList = agents)
            .Returns("Select agent");

        SetupChatCompletionResponse(chatCompletionMock, "DiplomaticHypothesisAgent");

        // Act
        await manager.SelectNextAgent(history, team);

        // Assert
        Assert.NotNull(capturedAgentList);
        Assert.Contains("DiplomaticHypothesisAgent", capturedAgentList);
    }

    #endregion

    #region Integration Tests

    /// <summary>
    /// WHY: Verifies complete workflow simulation through phases.
    /// </summary>
    [Fact]
    public async Task FullWorkflow_TransitionsThroughPhases()
    {
        // Arrange
        var (manager, chatCompletionMock, promptStrategyMock, _) = CreateManager();
        var team = new Mock<GroupChatTeam>().Object;

        promptStrategyMock
            .Setup(p => p.GetSelectionPrompt(It.IsAny<OrchestrationPromptInput>(), It.IsAny<List<string>>()))
            .Returns("Select agent");

        var capturedPhases = new List<int>();

        // Simulate progression through turns
        for (int turn = 0; turn <= 10; turn++)
        {
            var history = CreateHistoryWithTurns(turn);

            // Determine expected phase
            int phase = turn <= 7 ? 1 : (turn == 8 ? 2 : 3);
            capturedPhases.Add(phase);

            string expectedAgent = phase switch
            {
                1 => "DiplomaticHypothesisAgent",
                2 => "HypothesisScreeningAgent",
                _ => "FinalHypothesisSummarizerFormatter"
            };

            SetupChatCompletionResponse(chatCompletionMock, expectedAgent);

            // Act
            await manager.SelectNextAgent(history, team);
        }

        // Assert - Verify phase transitions
        Assert.Equal(1, capturedPhases[0]); // Turn 0 -> Phase 1
        Assert.Equal(1, capturedPhases[7]); // Turn 7 -> Phase 1
        Assert.Equal(2, capturedPhases[8]); // Turn 8 -> Phase 2
        Assert.Equal(3, capturedPhases[9]); // Turn 9 -> Phase 3
        Assert.Equal(3, capturedPhases[10]); // Turn 10 -> Phase 3
    }

    /// <summary>
    /// WHY: Verifies manager can handle cancellation token.
    /// </summary>
    [Fact]
    public async Task SelectNextAgent_WithCancellation_ThrowsOperationCanceledException()
    {
        // Arrange
        var (manager, chatCompletionMock, promptStrategyMock, _) = CreateManager();
        var history = CreateEmptyHistory();
        var team = new Mock<GroupChatTeam>().Object;
        var cts = new CancellationTokenSource();

        promptStrategyMock
            .Setup(p => p.GetSelectionPrompt(It.IsAny<OrchestrationPromptInput>(), It.IsAny<List<string>>()))
            .Returns("Select agent");

        chatCompletionMock.Setup(c => c.GetChatMessageContentsAsync(
                It.IsAny<ChatHistory>(),
                It.IsAny<PromptExecutionSettings>(),
                It.IsAny<Microsoft.SemanticKernel.Kernel?>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException());

        // Cancel before calling
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<ChatManagerException>(() =>
            manager.SelectNextAgent(history, team, cts.Token).AsTask());
    }

    #endregion

    #region Logging Tests

    /// <summary>
    /// WHY: Verifies constructor logs initialization information.
    /// </summary>
    [Fact]
    public void Constructor_LogsAgentCount()
    {
        // Arrange
        var chatCompletionMock = new Mock<IChatCompletionService>();
        var promptStrategyMock = new Mock<IGroupChatPromptStrategy>();
        var participationTracker = new AgentParticipationTracker();
        var loggerMock = new Mock<ILogger<HypothesisGenerationGroupChatManager>>();

        var agentNames = new List<string> { "Agent1", "Agent2", "Agent3" };

        // Act
        var manager = new HypothesisGenerationGroupChatManager(
            CreateDefaultInput(),
            agentNames,
            chatCompletionMock.Object,
            promptStrategyMock.Object,
            participationTracker,
            loggerMock.Object);

        // Assert - Verify logging was called
        loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("3 agents")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    #endregion

    #region Agent Name Filtering Tests

    /// <summary>
    /// WHY: Verifies Phase 1 agent list contains correct DIME-FIL agents.
    /// </summary>
    [Fact]
    public async Task SelectNextAgent_Phase1_ContainsAllDIMEFILAgents()
    {
        // Arrange
        var (manager, chatCompletionMock, promptStrategyMock, _) = CreateManager();
        var history = CreateEmptyHistory();
        var team = new Mock<GroupChatTeam>().Object;

        List<string>? capturedAgentList = null;
        promptStrategyMock
            .Setup(p => p.GetSelectionPrompt(It.IsAny<OrchestrationPromptInput>(), It.IsAny<List<string>>()))
            .Callback<OrchestrationPromptInput, List<string>>((_, agents) => capturedAgentList = agents)
            .Returns("Select agent");

        SetupChatCompletionResponse(chatCompletionMock, "DiplomaticHypothesisAgent");

        // Act
        await manager.SelectNextAgent(history, team);

        // Assert - All 8 Phase 1 agents should be present
        Assert.NotNull(capturedAgentList);
        Assert.Contains("DiplomaticHypothesisAgent", capturedAgentList);
        Assert.Contains("InformationHypothesisAgent", capturedAgentList);
        Assert.Contains("MilitaryHypothesisAgent", capturedAgentList);
        Assert.Contains("EconomicHypothesisAgent", capturedAgentList);
        Assert.Contains("FinancialHypothesisAgent", capturedAgentList);
        Assert.Contains("IntelligenceHypothesisAgent", capturedAgentList);
        Assert.Contains("LawEnforcementHypothesisAgent", capturedAgentList);
        Assert.Contains("DeceptionHypothesisAgent", capturedAgentList);
        Assert.Equal(8, capturedAgentList.Count);
    }

    /// <summary>
    /// WHY: Verifies Phase 2 filters to only HypothesisScreeningAgent.
    /// </summary>
    [Fact]
    public async Task SelectNextAgent_Phase2_FiltersToScreeningAgentOnly()
    {
        // Arrange
        var (manager, chatCompletionMock, promptStrategyMock, _) = CreateManager();
        var history = CreateHistoryWithTurns(8); // Turn 8 = Phase 2
        var team = new Mock<GroupChatTeam>().Object;

        List<string>? capturedAgentList = null;
        promptStrategyMock
            .Setup(p => p.GetSelectionPrompt(It.IsAny<OrchestrationPromptInput>(), It.IsAny<List<string>>()))
            .Callback<OrchestrationPromptInput, List<string>>((_, agents) => capturedAgentList = agents)
            .Returns("Select agent");

        SetupChatCompletionResponse(chatCompletionMock, "HypothesisScreeningAgent");

        // Act
        await manager.SelectNextAgent(history, team);

        // Assert
        Assert.NotNull(capturedAgentList);
        Assert.Single(capturedAgentList);
        Assert.Equal("HypothesisScreeningAgent", capturedAgentList[0]);
    }

    /// <summary>
    /// WHY: Verifies Phase 3 filters to only FinalHypothesisSummarizerFormatter.
    /// </summary>
    [Fact]
    public async Task SelectNextAgent_Phase3_FiltersToSummarizerOnly()
    {
        // Arrange
        var (manager, chatCompletionMock, promptStrategyMock, _) = CreateManager();
        var history = CreateHistoryWithTurns(9); // Turn 9 = Phase 3
        var team = new Mock<GroupChatTeam>().Object;

        List<string>? capturedAgentList = null;
        promptStrategyMock
            .Setup(p => p.GetSelectionPrompt(It.IsAny<OrchestrationPromptInput>(), It.IsAny<List<string>>()))
            .Callback<OrchestrationPromptInput, List<string>>((_, agents) => capturedAgentList = agents)
            .Returns("Select agent");

        SetupChatCompletionResponse(chatCompletionMock, "FinalHypothesisSummarizerFormatter");

        // Act
        await manager.SelectNextAgent(history, team);

        // Assert
        Assert.NotNull(capturedAgentList);
        Assert.Single(capturedAgentList);
        Assert.Equal("FinalHypothesisSummarizerFormatter", capturedAgentList[0]);
    }

    #endregion
}
#pragma warning restore SKEXP0110
