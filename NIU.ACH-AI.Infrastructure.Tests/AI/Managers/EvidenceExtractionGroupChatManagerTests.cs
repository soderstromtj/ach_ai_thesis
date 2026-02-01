using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Moq;
using NIU.ACH_AI.Application.Configuration;
using NIU.ACH_AI.Application.DTOs;
using NIU.ACH_AI.Application.Exceptions;
using NIU.ACH_AI.Application.Interfaces;
using NIU.ACH_AI.Infrastructure.AI.Managers;
using System.Text.Json;
using Microsoft.SemanticKernel.Agents.Orchestration.GroupChat;

namespace NIU.ACH_AI.Infrastructure.Tests.AI.Managers;

#pragma warning disable SKEXP0110 // Type is for evaluation purposes only

/// <summary>
/// Unit tests for EvidenceExtractionGroupChatManager.
/// </summary>
public class EvidenceExtractionGroupChatManagerTests
{
    private readonly Mock<IChatCompletionService> _chatCompletionMock;
    private readonly Mock<IGroupChatPromptStrategy> _promptStrategyMock;
    private readonly AgentParticipationTracker _participationTracker;
    private readonly Mock<ILogger<EvidenceExtractionGroupChatManager>> _loggerMock;
    private readonly OrchestrationPromptInput _input;
    private readonly List<string> _agentNames;

    public EvidenceExtractionGroupChatManagerTests()
    {
        _chatCompletionMock = new Mock<IChatCompletionService>();
        _promptStrategyMock = new Mock<IGroupChatPromptStrategy>();
        _participationTracker = new AgentParticipationTracker();
        _loggerMock = new Mock<ILogger<EvidenceExtractionGroupChatManager>>();

        _input = new OrchestrationPromptInput { KeyQuestion = "Test Question" };
        _agentNames = new List<string> { "AgentA", "AgentB" };
    }

    private EvidenceExtractionGroupChatManager CreateManager(OrchestrationSettings? settings = null)
    {
        return new EvidenceExtractionGroupChatManager(
            _input,
            _agentNames,
            _chatCompletionMock.Object,
            _promptStrategyMock.Object,
            _participationTracker,
            _loggerMock.Object)
        {
            MaximumInvocationCount = settings?.MaximumInvocationCount ?? 0
        };
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithValidArguments_CreatesInstance()
    {
        var manager = CreateManager();
        Assert.NotNull(manager);
    }

    [Fact]
    public void Constructor_NullArguments_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new EvidenceExtractionGroupChatManager(null!, _agentNames, _chatCompletionMock.Object, _promptStrategyMock.Object, _participationTracker, _loggerMock.Object));
        Assert.Throws<ArgumentNullException>(() => new EvidenceExtractionGroupChatManager(_input, _agentNames, null!, _promptStrategyMock.Object, _participationTracker, _loggerMock.Object));
    }

    #endregion

    #region ShouldTerminate Tests

    [Fact]
    public async Task ShouldTerminate_WhenMaxInvocationReached_ReturnsTrueWithoutLLM()
    {
        // Arrange
        var settings = new OrchestrationSettings { MaximumInvocationCount = 5 };
        var manager = CreateManager(settings);
        
        var history = new ChatHistory();
        for(int i=0; i<5; i++) history.AddAssistantMessage("msg");

        // Act
        var result = await manager.ShouldTerminate(history);

        // Assert
        Assert.True(result.Value);
        Assert.Contains("Maximum invocation limit", result.Reason);
        
        _chatCompletionMock.Verify(x => x.GetChatMessageContentsAsync(
            It.IsAny<ChatHistory>(), 
            It.IsAny<PromptExecutionSettings>(), 
            It.IsAny<Kernel>(), 
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ShouldTerminate_WhenUnderLimit_DelegatesToLLM_False()
    {
        // Arrange
        var manager = CreateManager();
        var history = new ChatHistory();
        
        _promptStrategyMock.Setup(s => s.GetTerminationPrompt(It.IsAny<OrchestrationPromptInput>(), It.IsAny<IEnumerable<string>>()))
            .Returns("TermPrompt");

        var llmResponse = JsonSerializer.Serialize(new GroupChatManagerResult<bool>(false) { Reason = "More work needed" });
        SetupChatCompletionResponse(llmResponse);

        // Act
        var result = await manager.ShouldTerminate(history);

        // Assert
        Assert.False(result.Value);
    }

    [Fact]
    public async Task ShouldTerminate_WhenUnderLimitAndAllAgentsParticipated_DelegatesToLLM_True()
    {
        // Arrange
        var manager = CreateManager(); 
        var history = new ChatHistory();
        history.Add(new ChatMessageContent(AuthorRole.Assistant, "Message from AgentA") { AuthorName = "AgentA" });
        history.Add(new ChatMessageContent(AuthorRole.Assistant, "Message from AgentB") { AuthorName = "AgentB" });
        
        _promptStrategyMock.Setup(s => s.GetTerminationPrompt(It.IsAny<OrchestrationPromptInput>(), It.IsAny<IEnumerable<string>>()))
            .Returns("TermPrompt");

        var llmResponse = JsonSerializer.Serialize(new GroupChatManagerResult<bool>(true) { Reason = "Done" });
        SetupChatCompletionResponse(llmResponse);

        // Act
        var result = await manager.ShouldTerminate(history);

        // Assert
        Assert.True(result.Value);
        _chatCompletionMock.Verify(x => x.GetChatMessageContentsAsync(
            It.IsAny<ChatHistory>(),
            It.IsAny<PromptExecutionSettings>(),
            It.IsAny<Kernel>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region FilterResults Tests

    [Fact]
    public async Task FilterResults_CallsPromptStrategyAndParsesResponse()
    {
        // Arrange
        var manager = CreateManager();
        var history = new ChatHistory();
        var expectedResult = "Filtered Result Content";

        _promptStrategyMock.Setup(s => s.GetFilterPrompt(It.IsAny<OrchestrationPromptInput>()))
            .Returns("FilterPrompt");

        var llmResponse = JsonSerializer.Serialize(new GroupChatManagerResult<string>(expectedResult));
        SetupChatCompletionResponse(llmResponse);

        // Act
        var result = await manager.FilterResults(history);

        // Assert
        Assert.Equal(expectedResult, result.Value);
        _promptStrategyMock.Verify(s => s.GetFilterPrompt(_input), Times.Once);
    }

    [Fact]
    public async Task FilterResults_WhenLLMReturnsInvalidJSON_ThrowsChatManagerException()
    {
        // Arrange
        var manager = CreateManager();
        var history = new ChatHistory();
        _promptStrategyMock.Setup(s => s.GetFilterPrompt(It.IsAny<OrchestrationPromptInput>())).Returns("FilterPrompt");
        SetupChatCompletionResponse("Invalid JSON string");

        // Act & Assert
        await Assert.ThrowsAsync<ChatManagerException>(async () => await manager.FilterResults(history));
    }

    #endregion

    #region Helper Methods

    private void SetupChatCompletionResponse(string content)
    {
        var chatMessage = new ChatMessageContent(AuthorRole.Assistant, content);
        IReadOnlyList<ChatMessageContent> response = new List<ChatMessageContent> { chatMessage };
        _chatCompletionMock
            .Setup(x => x.GetChatMessageContentsAsync(
                It.IsAny<ChatHistory>(),
                It.IsAny<PromptExecutionSettings>(),
                It.IsAny<Kernel>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(response));
    }

    #endregion
}

#pragma warning restore SKEXP0110
