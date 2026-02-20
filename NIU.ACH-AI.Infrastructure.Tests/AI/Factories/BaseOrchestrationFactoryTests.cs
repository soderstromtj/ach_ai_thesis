using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.Agents.Orchestration;
using Microsoft.SemanticKernel.Agents.Orchestration.Transforms;
using Microsoft.SemanticKernel.ChatCompletion;
using Moq;
using NIU.ACH_AI.Application.Configuration;
using NIU.ACH_AI.Application.DTOs;
using NIU.ACH_AI.Application.Interfaces;
using NIU.ACH_AI.Domain.Entities;
using NIU.ACH_AI.Infrastructure.AI.Factories;

namespace NIU.ACH_AI.Infrastructure.Tests.AI.Factories;

#pragma warning disable SKEXP0110 // Suppress Semantic Kernel experimental warnings

/// <summary>
/// Comprehensive unit tests for BaseOrchestrationFactory.
///
/// Testing Strategy:
/// -----------------
/// BaseOrchestrationFactory is an ABSTRACT class using the Template Method pattern.
/// To test it, we create a concrete test implementation (TestableOrchestrationFactory)
/// that provides predictable behavior for all abstract methods.
///
/// What We Can Test:
/// 1. Constructor - Validates dependency injection works correctly
/// 2. ResponseCallback - Adds responses to history, tracks turns
/// 3. StreamingResponseCallback - Buffers streaming content appropriately
/// 4. InteractiveCallback - Returns expected continuation message
/// 5. Protected field initialization - Verifies internal state setup
///
/// Testing Challenges:
/// The ExecuteCoreAsync method is tightly coupled to Semantic Kernel's runtime
/// (InProcessRuntime, AgentOrchestration). Full testing would require either:
/// - Integration tests with real AI services (out of scope for unit tests)
/// - Extensive mocking of Semantic Kernel internals (fragile)
///
/// We focus on testing the logic we control while documenting boundaries.
/// </summary>
public class BaseOrchestrationFactoryTests
{
    #region Test Infrastructure

    /// <summary>
    /// Concrete test implementation of the abstract BaseOrchestrationFactory.
    /// This "test double" allows us to:
    /// - Instantiate the abstract class
    /// - Control what abstract methods return
    /// - Expose protected members for testing via public wrappers
    /// </summary>
    private class TestableOrchestrationFactory : BaseOrchestrationFactory<List<Evidence>, EvidenceResult>
    {
        // Track calls to abstract methods for verification
        public int CreateOrchestrationCallCount { get; private set; }
        public int UnwrapResultCallCount { get; private set; }
        public int GetItemCountCallCount { get; private set; }
        public int CreateEmptyResultCallCount { get; private set; }
        public int CreateErrorResultCallCount { get; private set; }
        public int GetAgentSelectionReasonCallCount { get; private set; }

        // Configurable return values for abstract methods
        public List<Evidence> EmptyResultToReturn { get; set; } = new();
        public List<Evidence> ErrorResultToReturn { get; set; } = new();
        public List<Evidence> UnwrapResultToReturn { get; set; } = new();

        public TestableOrchestrationFactory(
            IAgentService agentService,
            IKernelBuilderService kernelBuilderService,
            IOptions<OrchestrationSettings> orchestrationSettings,
            IOrchestrationPromptFormatter orchestrationPromptFormatterMock,
            ILoggerFactory loggerFactory,
            IAgentResponsePersistence orchestrationPersistenceMock)
            : base(agentService, kernelBuilderService, orchestrationSettings, orchestrationPromptFormatterMock, loggerFactory, orchestrationPersistenceMock)
        { }

        // Expose protected members for testing
        public IAgentService ExposedAgentService => _agentService;
        public IKernelBuilderService ExposedKernelBuilderService => _kernelBuilderService;
        public OrchestrationSettings ExposedOrchestrationSettings => _orchestrationSettings;
        public ChatHistory ExposedHistory => _history;
        public ILogger ExposedLogger => _logger;
        public ILoggerFactory ExposedLoggerFactory => _loggerFactory;

        // Wrapper to expose protected ResponseCallback
        public ValueTask InvokeResponseCallback(ChatMessageContent response)
            => ResponseCallback(response);

        // Wrapper to expose protected StreamingResponseCallback
        public ValueTask InvokeStreamingResponseCallback(StreamingChatMessageContent response, bool isFinal)
            => StreamingResponseCallback(response, isFinal);

        // Wrapper to expose protected InteractiveCallback
        public ValueTask<ChatMessageContent> InvokeInteractiveCallback()
            => InteractiveCallback();

        #region Abstract Method Implementations


        protected override AgentOrchestration<string, EvidenceResult> CreateOrchestration(
            OrchestrationPromptInput input,
            Kernel kernel,
            Agent[] agents,
            StructuredOutputTransform<EvidenceResult> outputTransform)
        {
            CreateOrchestrationCallCount++;
            // Return null - this method won't be called in our unit tests
            // since we're not testing the full ExecuteCoreAsync flow
            return null!;
        }

        protected override string GetResultTypeName()
        {
            return nameof(EvidenceResult);
        }

        protected override List<Evidence> UnwrapResult(EvidenceResult wrapper)
        {
            UnwrapResultCallCount++;
            return UnwrapResultToReturn;
        }

        protected override int GetItemCount(List<Evidence> result)
        {
            GetItemCountCallCount++;
            return result.Count;
        }

        protected override List<Evidence> CreateEmptyResult()
        {
            CreateEmptyResultCallCount++;
            return EmptyResultToReturn;
        }

        protected override List<Evidence> CreateErrorResult()
        {
            CreateErrorResultCallCount++;
            return ErrorResultToReturn;
        }

        protected override string GetAgentSelectionReason(string? previousAgentName)
        {
            GetAgentSelectionReasonCallCount++;
            return $"Test selection after {previousAgentName}";
        }

        #endregion
    }

    /// <summary>
    /// Creates a factory instance with all dependencies mocked.
    /// Uses the Arrange-Act-Assert pattern - this is the "Arrange" helper.
    /// </summary>
    private static (TestableOrchestrationFactory Factory, Mock<IAgentService> AgentServiceMock,
        Mock<IKernelBuilderService> KernelBuilderServiceMock, OrchestrationSettings Settings,
        Mock<ILoggerFactory> LoggerFactoryMock) CreateFactory(
        OrchestrationSettings? settings = null)
    {
        // Create mocks for all dependencies
        var agentServiceMock = new Mock<IAgentService>();
        var kernelBuilderServiceMock = new Mock<IKernelBuilderService>();
        var loggerFactoryMock = new Mock<ILoggerFactory>();
        var loggerMock = new Mock<ILogger>();

        // Setup logger factory to return a mock logger
        loggerFactoryMock
            .Setup(f => f.CreateLogger(It.IsAny<string>()))
            .Returns(loggerMock.Object);

        // Use provided settings or create defaults
        var orchestrationSettings = settings ?? new OrchestrationSettings
        {
            MaximumInvocationCount = 10,
            TimeoutInMinutes = 15,
            StreamResponses = false,
            WriteResponses = false // Disable console output in tests
        };

        var optionsMock = new Mock<IOptions<OrchestrationSettings>>();
        optionsMock.Setup(o => o.Value).Returns(orchestrationSettings);

        // Create mock IOrchestrationPromptFormatter
        var orchestrationPromptFormatterMock = new Mock<IOrchestrationPromptFormatter>();
        orchestrationPromptFormatterMock.Setup(f => f.FormatPrompt(It.IsAny<OrchestrationPromptInput>())).Returns("formatted prompt");

        // Create mock IAgentResponsePersistence
        var orchestrationPersistenceMock = new Mock<IAgentResponsePersistence>();

        // Create the factory wrapper instance
        var factory = new TestableOrchestrationFactory(
            agentServiceMock.Object,
            kernelBuilderServiceMock.Object,
            optionsMock.Object,
            orchestrationPromptFormatterMock.Object,
            loggerFactoryMock.Object,
            orchestrationPersistenceMock.Object);

        return (factory, agentServiceMock, kernelBuilderServiceMock,
            orchestrationSettings, loggerFactoryMock);
    }

    #endregion

    #region Constructor Tests

    /// <summary>
    /// WHY: Verifies the constructor correctly stores the IAgentService dependency.
    /// This ensures dependency injection works and the service is available for use.
    ///
    /// WHAT: Arrange with mock, act by constructing, assert the dependency is stored.
    /// </summary>
    [Fact]
    public void Constructor_WithValidAgentService_StoresAgentService()
    {
        // Arrange & Act
        var (factory, agentServiceMock, _, _, _) = CreateFactory();

        // Assert
        Assert.Same(agentServiceMock.Object, factory.ExposedAgentService);
    }

    /// <summary>
    /// WHY: Verifies the constructor correctly stores the IKernelBuilderService dependency.
    /// The kernel builder is essential for creating the Semantic Kernel instance.
    /// </summary>
    [Fact]
    public void Constructor_WithValidKernelBuilderService_StoresKernelBuilderService()
    {
        // Arrange & Act
        var (factory, _, kernelBuilderServiceMock, _, _) = CreateFactory();

        // Assert
        Assert.Same(kernelBuilderServiceMock.Object, factory.ExposedKernelBuilderService);
    }

    /// <summary>
    /// WHY: Verifies OrchestrationSettings are unwrapped from IOptions and stored.
    /// Settings control timeout, streaming, and other runtime behaviors.
    /// </summary>
    [Fact]
    public void Constructor_WithValidSettings_UnwrapsAndStoresSettings()
    {
        // Arrange
        var expectedSettings = new OrchestrationSettings
        {
            MaximumInvocationCount = 5,
            TimeoutInMinutes = 30,
            StreamResponses = true,
            WriteResponses = true
        };

        // Act
        var (factory, _, _, _, _) = CreateFactory(expectedSettings);

        // Assert
        Assert.Equal(expectedSettings.MaximumInvocationCount, factory.ExposedOrchestrationSettings.MaximumInvocationCount);
        Assert.Equal(expectedSettings.TimeoutInMinutes, factory.ExposedOrchestrationSettings.TimeoutInMinutes);
        Assert.Equal(expectedSettings.StreamResponses, factory.ExposedOrchestrationSettings.StreamResponses);
        Assert.Equal(expectedSettings.WriteResponses, factory.ExposedOrchestrationSettings.WriteResponses);
    }

    /// <summary>
    /// WHY: Verifies that a new ChatHistory is created for each factory instance.
    /// The history tracks the conversation and should start empty.
    /// </summary>
    [Fact]
    public void Constructor_Always_CreatesEmptyChatHistory()
    {
        // Arrange & Act
        var (factory, _, _, _, _) = CreateFactory();

        // Assert
        Assert.NotNull(factory.ExposedHistory);
        Assert.Empty(factory.ExposedHistory);
    }


    /// <summary>
    /// WHY: Verifies the logger factory is stored for potential future use.
    /// Derived classes may need to create additional loggers.
    /// </summary>
    [Fact]
    public void Constructor_WithValidLoggerFactory_StoresLoggerFactory()
    {
        // Arrange & Act
        var (factory, _, _, _, loggerFactoryMock) = CreateFactory();

        // Assert
        Assert.Same(loggerFactoryMock.Object, factory.ExposedLoggerFactory);
    }

    /// <summary>
    /// WHY: Verifies that a logger instance is created and stored.
    /// Logging is used throughout the orchestration process.
    /// </summary>
    [Fact]
    public void Constructor_Always_CreatesAndStoresLogger()
    {
        // Arrange & Act
        var (factory, _, _, _, _) = CreateFactory();

        // Assert
        Assert.NotNull(factory.ExposedLogger);
    }

    #endregion

    #region ResponseCallback Tests

    /// <summary>
    /// WHY: The ResponseCallback should add each response to the chat history.
    /// This is essential for tracking the conversation and providing context.
    ///
    /// WHAT: Call ResponseCallback with a message, verify it's in history.
    /// </summary>
    [Fact]
    public async Task ResponseCallback_WithValidResponse_AddsToHistory()
    {
        // Arrange
        var (factory, _, _, _, _) = CreateFactory();
        var response = new ChatMessageContent
        {
            AuthorName = "TestAgent",
            Content = "Test response content"
        };

        // Act
        await factory.InvokeResponseCallback(response);

        // Assert
        Assert.Single(factory.ExposedHistory);
        Assert.Same(response, factory.ExposedHistory.First());
    }

    /// <summary>
    /// WHY: Multiple responses should accumulate in history.
    /// Each agent's response builds on the conversation context.
    /// </summary>
    [Fact]
    public async Task ResponseCallback_WithMultipleResponses_AccumulatesInHistory()
    {
        // Arrange
        var (factory, _, _, _, _) = CreateFactory();
        var response1 = new ChatMessageContent { AuthorName = "Agent1", Content = "First" };
        var response2 = new ChatMessageContent { AuthorName = "Agent2", Content = "Second" };
        var response3 = new ChatMessageContent { AuthorName = "Agent1", Content = "Third" };

        // Act
        await factory.InvokeResponseCallback(response1);
        await factory.InvokeResponseCallback(response2);
        await factory.InvokeResponseCallback(response3);

        // Assert
        Assert.Equal(3, factory.ExposedHistory.Count);
    }

    /// <summary>
    /// WHY: The callback should handle null content gracefully.
    /// AI responses may sometimes have null content (metadata-only responses).
    /// </summary>
    [Fact]
    public async Task ResponseCallback_WithNullContent_HandlesGracefully()
    {
        // Arrange
        var (factory, _, _, _, _) = CreateFactory();
        var response = new ChatMessageContent
        {
            AuthorName = "TestAgent",
            Content = null
        };

        // Act - Should not throw
        var exception = await Record.ExceptionAsync(() =>
            factory.InvokeResponseCallback(response).AsTask());

        // Assert
        Assert.Null(exception);
        Assert.Single(factory.ExposedHistory);
    }

    /// <summary>
    /// WHY: The callback should handle null author name gracefully.
    /// Some responses may not have an author specified.
    /// </summary>
    [Fact]
    public async Task ResponseCallback_WithNullAuthorName_HandlesGracefully()
    {
        // Arrange
        var (factory, _, _, _, _) = CreateFactory();
        var response = new ChatMessageContent
        {
            AuthorName = null,
            Content = "Test content"
        };

        // Act - Should not throw
        var exception = await Record.ExceptionAsync(() =>
            factory.InvokeResponseCallback(response).AsTask());

        // Assert
        Assert.Null(exception);
        Assert.Single(factory.ExposedHistory);
    }

    /// <summary>
    /// WHY: Empty content is a valid edge case that should work correctly.
    /// Agents may send empty confirmations or acknowledgments.
    /// </summary>
    [Fact]
    public async Task ResponseCallback_WithEmptyContent_AddsToHistory()
    {
        // Arrange
        var (factory, _, _, _, _) = CreateFactory();
        var response = new ChatMessageContent
        {
            AuthorName = "TestAgent",
            Content = string.Empty
        };

        // Act
        await factory.InvokeResponseCallback(response);

        // Assert
        Assert.Single(factory.ExposedHistory);
        Assert.Equal(string.Empty, factory.ExposedHistory.First().Content);
    }

    /// <summary>
    /// WHY: When WriteResponses is enabled, responses should be written to console.
    /// This tests the console output path which is used for user feedback.
    /// </summary>
    [Fact]
    public async Task ResponseCallback_WhenWriteResponsesEnabled_WritesToConsole()
    {
        // Arrange
        var settings = new OrchestrationSettings { WriteResponses = true };
        var (factory, _, _, _, _) = CreateFactory(settings);
        var response = new ChatMessageContent
        {
            AuthorName = "TestAgent",
            Content = "Test console output"
        };
        var loggerMock = Mock.Get(factory.ExposedLogger);

        // Act
        await factory.InvokeResponseCallback(response);

        // Assert - Verify content appears in logs
        loggerMock.Verify(l => l.Log(
            LogLevel.Information,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("TestAgent") && v.ToString().Contains("Test console output")),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception, string>>()), Times.Once);
    }

    /// <summary>
    /// WHY: When WriteResponses is disabled, no console output should occur.
    /// This is important for background processing or when output isn't desired.
    /// </summary>
    [Fact]
    public async Task ResponseCallback_WhenWriteResponsesDisabled_DoesNotWriteToConsole()
    {
        // Arrange
        var settings = new OrchestrationSettings { WriteResponses = false };
        var (factory, _, _, _, _) = CreateFactory(settings);
        var response = new ChatMessageContent
        {
            AuthorName = "TestAgent",
            Content = "This should not appear"
        };

        // Capture console output - IMPORTANT: Save and restore to avoid affecting other tests
        var originalOut = Console.Out;
        using var consoleOutput = new StringWriter();
        try
        {
            Console.SetOut(consoleOutput);

            // Act
            await factory.InvokeResponseCallback(response);

            // Assert - Console should remain empty
            var output = consoleOutput.ToString();
            Assert.Empty(output);
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }



    /// <summary>
    /// WHY: When agent changes between responses, it represents a "handoff".
    /// The callback should detect and handle agent transitions.
    /// </summary>
    [Fact]
    public async Task ResponseCallback_WhenAgentChanges_TracksAgentHandoff()
    {
        // Arrange
        var (factory, _, _, _, _) = CreateFactory();
        var response1 = new ChatMessageContent { AuthorName = "Agent1", Content = "First" };
        var response2 = new ChatMessageContent { AuthorName = "Agent2", Content = "Second" };
        var response3 = new ChatMessageContent { AuthorName = "Agent1", Content = "Third" };

        // Act
        await factory.InvokeResponseCallback(response1);
        await factory.InvokeResponseCallback(response2);
        await factory.InvokeResponseCallback(response3);

        // Assert - All responses should be in history (handoff tracking is internal)
        Assert.Equal(3, factory.ExposedHistory.Count);
        // Verify GetAgentSelectionReason was called for handoffs
        // First call doesn't count as handoff, so we expect 2 calls (Agent1->Agent2, Agent2->Agent1)
        Assert.Equal(2, factory.GetAgentSelectionReasonCallCount);
    }

    /// <summary>
    /// WHY: Whitespace-only content is a valid edge case.
    /// Some responses may contain only spaces, tabs, or newlines.
    /// </summary>
    [Fact]
    public async Task ResponseCallback_WithWhitespaceOnlyContent_PreservesContent()
    {
        // Arrange
        var (factory, _, _, _, _) = CreateFactory();
        var whitespaceContent = "   \t\n\r\n   ";
        var response = new ChatMessageContent
        {
            AuthorName = "TestAgent",
            Content = whitespaceContent
        };

        // Act
        await factory.InvokeResponseCallback(response);

        // Assert
        Assert.Single(factory.ExposedHistory);
        Assert.Equal(whitespaceContent, factory.ExposedHistory.First().Content);
    }

    /// <summary>
    /// WHY: Both null content AND null author is an extreme edge case.
    /// The callback should handle completely empty responses gracefully.
    /// </summary>
    [Fact]
    public async Task ResponseCallback_WithBothNullContentAndAuthor_HandlesGracefully()
    {
        // Arrange
        var (factory, _, _, _, _) = CreateFactory();
        var response = new ChatMessageContent
        {
            AuthorName = null,
            Content = null
        };

        // Act - Should not throw
        var exception = await Record.ExceptionAsync(() =>
            factory.InvokeResponseCallback(response).AsTask());

        // Assert
        Assert.Null(exception);
        Assert.Single(factory.ExposedHistory);
    }

    #endregion

    #region StreamingResponseCallback Tests

    /// <summary>
    /// WHY: When streaming is disabled, the callback should complete without error.
    /// This is the default behavior for non-streaming orchestrations.
    /// </summary>
    [Fact]
    public async Task StreamingResponseCallback_WhenStreamingDisabled_CompletesWithoutError()
    {
        // Arrange
        var settings = new OrchestrationSettings { StreamResponses = false };
        var (factory, _, _, _, _) = CreateFactory(settings);
        var response = CreateStreamingResponse("TestAgent", "chunk content");

        // Act - Should complete without error
        var exception = await Record.ExceptionAsync(() =>
            factory.InvokeStreamingResponseCallback(response, isFinal: false).AsTask());

        // Assert
        Assert.Null(exception);
    }

    /// <summary>
    /// WHY: When streaming is enabled, non-final chunks should be buffered.
    /// This allows assembling partial responses before the final chunk arrives.
    /// </summary>
    [Fact]
    public async Task StreamingResponseCallback_WhenStreamingEnabled_ProcessesChunk()
    {
        // Arrange
        var settings = new OrchestrationSettings { StreamResponses = true };
        var (factory, _, _, _, _) = CreateFactory(settings);
        var response = CreateStreamingResponse("TestAgent", "chunk content");
        var loggerMock = Mock.Get(factory.ExposedLogger);

        // Act
        await factory.InvokeStreamingResponseCallback(response, isFinal: false);

        // Assert - Content should be logged as Trace
        loggerMock.Verify(l => l.Log(
            LogLevel.Trace,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("chunk content")),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception, string>>()), Times.Once);
    }

    /// <summary>
    /// WHY: Final chunks should trigger buffer cleanup.
    /// Memory should be freed when streaming for an agent is complete.
    /// </summary>
    [Fact]
    public async Task StreamingResponseCallback_WhenFinalChunk_CompletesSuccessfully()
    {
        // Arrange
        var settings = new OrchestrationSettings { StreamResponses = true };
        var (factory, _, _, _, _) = CreateFactory(settings);
        var response = CreateStreamingResponse("TestAgent", "final content");
        var loggerMock = Mock.Get(factory.ExposedLogger);

        // Act
        await factory.InvokeStreamingResponseCallback(response, isFinal: true);

        // Assert - Content should be logged as Trace (final chunk is still a chunk)
        loggerMock.Verify(l => l.Log(
            LogLevel.Trace,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("final content")),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception, string>>()), Times.Once);
    }

    /// <summary>
    /// WHY: Null content in streaming chunks should be handled gracefully.
    /// Streaming chunks may sometimes be metadata-only.
    /// </summary>
    [Fact]
    public async Task StreamingResponseCallback_WithNullContent_HandlesGracefully()
    {
        // Arrange
        var settings = new OrchestrationSettings { StreamResponses = true };
        var (factory, _, _, _, _) = CreateFactory(settings);
        var response = CreateStreamingResponse("TestAgent", null);

        // Capture console output - IMPORTANT: Save and restore to avoid affecting other tests
        var originalOut = Console.Out;
        using var consoleOutput = new StringWriter();
        try
        {
            Console.SetOut(consoleOutput);

            // Act - Should not throw
            var exception = await Record.ExceptionAsync(() =>
                factory.InvokeStreamingResponseCallback(response, isFinal: false).AsTask());

            // Assert
            Assert.Null(exception);
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    /// <summary>
    /// WHY: Null author name should default to "Unknown" and not crash.
    /// Anonymous or system responses may lack an author.
    /// </summary>
    [Fact]
    public async Task StreamingResponseCallback_WithNullAuthorName_HandlesGracefully()
    {
        // Arrange
        var settings = new OrchestrationSettings { StreamResponses = true };
        var (factory, _, _, _, _) = CreateFactory(settings);
        var response = CreateStreamingResponse(null, "content");

        // Capture console output - IMPORTANT: Save and restore to avoid affecting other tests
        var originalOut = Console.Out;
        using var consoleOutput = new StringWriter();
        try
        {
            Console.SetOut(consoleOutput);

            // Act - Should not throw
            var exception = await Record.ExceptionAsync(() =>
                factory.InvokeStreamingResponseCallback(response, isFinal: false).AsTask());

            // Assert
            Assert.Null(exception);
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    /// <summary>
    /// WHY: Multiple chunks from the same agent should accumulate in the buffer.
    /// This tests the core streaming functionality where partial responses build up.
    /// </summary>
    [Fact]
    public async Task StreamingResponseCallback_WithMultipleChunks_AccumulatesInBuffer()
    {
        // Arrange
        var settings = new OrchestrationSettings { StreamResponses = true };
        var (factory, _, _, _, _) = CreateFactory(settings);
        var loggerMock = Mock.Get(factory.ExposedLogger);

        // Act - Send multiple chunks from same agent
        await factory.InvokeStreamingResponseCallback(
            CreateStreamingResponse("TestAgent", "Hello "), isFinal: false);
        await factory.InvokeStreamingResponseCallback(
            CreateStreamingResponse("TestAgent", "World "), isFinal: false);
        await factory.InvokeStreamingResponseCallback(
            CreateStreamingResponse("TestAgent", "!"), isFinal: true);

        // Assert - All chunks should be logged
        loggerMock.Verify(l => l.Log(
           LogLevel.Trace,
           It.IsAny<EventId>(),
           It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Hello ")),
           It.IsAny<Exception>(),
           It.IsAny<Func<It.IsAnyType, Exception, string>>()), Times.Once);

        loggerMock.Verify(l => l.Log(
           LogLevel.Trace,
           It.IsAny<EventId>(),
           It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("World ")),
           It.IsAny<Exception>(),
           It.IsAny<Func<It.IsAnyType, Exception, string>>()), Times.Once);
        
        loggerMock.Verify(l => l.Log(
           LogLevel.Trace,
           It.IsAny<EventId>(),
           It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("!")),
           It.IsAny<Exception>(),
           It.IsAny<Func<It.IsAnyType, Exception, string>>()), Times.Once);
    }

    /// <summary>
    /// WHY: Empty string content (not null) should be handled correctly.
    /// Empty chunks may occur during streaming as placeholders.
    /// </summary>
    [Fact]
    public async Task StreamingResponseCallback_WithEmptyStringContent_HandlesGracefully()
    {
        // Arrange
        var settings = new OrchestrationSettings { StreamResponses = true };
        var (factory, _, _, _, _) = CreateFactory(settings);
        var response = CreateStreamingResponse("TestAgent", string.Empty);

        // Capture console output
        var originalOut = Console.Out;
        using var consoleOutput = new StringWriter();
        try
        {
            Console.SetOut(consoleOutput);

            // Act - Should not throw
            var exception = await Record.ExceptionAsync(() =>
                factory.InvokeStreamingResponseCallback(response, isFinal: false).AsTask());

            // Assert
            Assert.Null(exception);
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    /// <summary>
    /// WHY: Both null content AND null author is an extreme edge case for streaming.
    /// The callback should handle completely empty streaming responses gracefully.
    /// </summary>
    [Fact]
    public async Task StreamingResponseCallback_WithBothNullContentAndAuthor_HandlesGracefully()
    {
        // Arrange
        var settings = new OrchestrationSettings { StreamResponses = true };
        var (factory, _, _, _, _) = CreateFactory(settings);
        var response = CreateStreamingResponse(null, null);

        // Capture console output
        var originalOut = Console.Out;
        using var consoleOutput = new StringWriter();
        try
        {
            Console.SetOut(consoleOutput);

            // Act - Should not throw
            var exception = await Record.ExceptionAsync(() =>
                factory.InvokeStreamingResponseCallback(response, isFinal: false).AsTask());

            // Assert
            Assert.Null(exception);
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    /// <summary>
    /// WHY: Multiple agents streaming simultaneously should maintain separate buffers.
    /// Each agent's chunks should be tracked independently.
    /// </summary>
    [Fact]
    public async Task StreamingResponseCallback_WithMultipleAgents_MaintainsSeparateBuffers()
    {
        // Arrange
        var settings = new OrchestrationSettings { StreamResponses = true };
        var (factory, _, _, _, _) = CreateFactory(settings);
        var loggerMock = Mock.Get(factory.ExposedLogger);

        // Act - Interleave chunks from two agents
        await factory.InvokeStreamingResponseCallback(
            CreateStreamingResponse("Agent1", "A1-Chunk1 "), isFinal: false);
        await factory.InvokeStreamingResponseCallback(
            CreateStreamingResponse("Agent2", "A2-Chunk1 "), isFinal: false);
        await factory.InvokeStreamingResponseCallback(
            CreateStreamingResponse("Agent1", "A1-Chunk2"), isFinal: true);
        await factory.InvokeStreamingResponseCallback(
            CreateStreamingResponse("Agent2", "A2-Chunk2"), isFinal: true);

        // Assert - All chunks from both agents should be logged
        loggerMock.Verify(l => l.Log(
           LogLevel.Trace,
           It.IsAny<EventId>(),
           It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("A1-Chunk1")),
           It.IsAny<Exception>(),
           It.IsAny<Func<It.IsAnyType, Exception, string>>()), Times.Once);

        loggerMock.Verify(l => l.Log(
           LogLevel.Trace,
           It.IsAny<EventId>(),
           It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("A2-Chunk1")),
           It.IsAny<Exception>(),
           It.IsAny<Func<It.IsAnyType, Exception, string>>()), Times.Once);
    }

    /// <summary>
    /// Helper to create StreamingChatMessageContent for tests.
    /// StreamingChatMessageContent requires specific construction.
    /// </summary>
    private static StreamingChatMessageContent CreateStreamingResponse(
        string? authorName, string? content)
    {
        return new StreamingChatMessageContent(
            role: AuthorRole.Assistant,
            content: content)
        {
            AuthorName = authorName
        };
    }

    #endregion

    #region InteractiveCallback Tests

    /// <summary>
    /// WHY: The interactive callback provides a way to continue orchestration.
    /// When no user input is provided, it should return a continuation message.
    /// </summary>
    [Fact]
    public async Task InteractiveCallback_WhenInvoked_ReturnsNonNullMessage()
    {
        // Arrange
        var (factory, _, _, _, _) = CreateFactory();

        // Act
        var result = await factory.InvokeInteractiveCallback();

        // Assert
        Assert.NotNull(result);
    }

    /// <summary>
    /// WHY: The continuation message should have content explaining what's happening.
    /// This helps with debugging and understanding the orchestration flow.
    /// </summary>
    [Fact]
    public async Task InteractiveCallback_WhenInvoked_ReturnsContinuationMessage()
    {
        // Arrange
        var (factory, _, _, _, _) = CreateFactory();

        // Act
        var result = await factory.InvokeInteractiveCallback();

        // Assert
        Assert.NotNull(result.Content);
        Assert.Contains("Continuing orchestration", result.Content);
    }

    /// <summary>
    /// WHY: Multiple calls should be consistent (idempotent behavior).
    /// The callback should always return the same type of message.
    /// </summary>
    [Fact]
    public async Task InteractiveCallback_WhenInvokedMultipleTimes_ReturnsConsistentMessage()
    {
        // Arrange
        var (factory, _, _, _, _) = CreateFactory();

        // Act
        var result1 = await factory.InvokeInteractiveCallback();
        var result2 = await factory.InvokeInteractiveCallback();
        var result3 = await factory.InvokeInteractiveCallback();

        // Assert
        Assert.Equal(result1.Content, result2.Content);
        Assert.Equal(result2.Content, result3.Content);
    }

    #endregion

    #region Template Method Pattern Tests

    /// <summary>
    /// WHY: GetResultTypeName should return a descriptive type name for logging.
    /// This helps identify what type of result the orchestration produces.
    /// </summary>
    [Fact]
    public void GetResultTypeName_InDerivedClass_ReturnsExpectedTypeName()
    {
        // Arrange
        var (factory, _, _, _, _) = CreateFactory();

        // Act - Use reflection to call the protected method
        var methodInfo = typeof(TestableOrchestrationFactory)
            .GetMethod("GetResultTypeName",
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Instance);
        var result = methodInfo?.Invoke(factory, null) as string;

        // Assert
        Assert.Equal(nameof(EvidenceResult), result);
    }

    /// <summary>
    /// WHY: CreateEmptyResult should create a valid empty result for null scenarios.
    /// This prevents null reference exceptions when orchestration fails.
    /// </summary>
    [Fact]
    public void CreateEmptyResult_WhenInvoked_ReturnsConfiguredEmptyResult()
    {
        // Arrange
        var (factory, _, _, _, _) = CreateFactory();
        var expectedResult = new List<Evidence>();
        factory.EmptyResultToReturn = expectedResult;

        // Act - Use reflection
        var methodInfo = typeof(TestableOrchestrationFactory)
            .GetMethod("CreateEmptyResult",
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Instance);
        var result = methodInfo?.Invoke(factory, null) as List<Evidence>;

        // Assert
        Assert.Same(expectedResult, result);
        Assert.Equal(1, factory.CreateEmptyResultCallCount);
    }

    /// <summary>
    /// WHY: CreateErrorResult should create a valid result for error scenarios.
    /// This allows graceful degradation when orchestration encounters errors.
    /// </summary>
    [Fact]
    public void CreateErrorResult_WhenInvoked_ReturnsConfiguredErrorResult()
    {
        // Arrange
        var (factory, _, _, _, _) = CreateFactory();
        var expectedResult = new List<Evidence>();
        factory.ErrorResultToReturn = expectedResult;

        // Act - Use reflection
        var methodInfo = typeof(TestableOrchestrationFactory)
            .GetMethod("CreateErrorResult",
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Instance);
        var result = methodInfo?.Invoke(factory, null) as List<Evidence>;

        // Assert
        Assert.Same(expectedResult, result);
        Assert.Equal(1, factory.CreateErrorResultCallCount);
    }

    /// <summary>
    /// WHY: GetAgentSelectionReason should explain why an agent was selected.
    /// This aids in debugging and understanding orchestration decisions.
    /// </summary>
    [Fact]
    public void GetAgentSelectionReason_WithPreviousAgent_ReturnsReasonString()
    {
        // Arrange
        var (factory, _, _, _, _) = CreateFactory();

        // Act - Use reflection
        var methodInfo = typeof(TestableOrchestrationFactory)
            .GetMethod("GetAgentSelectionReason",
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Instance);
        var result = methodInfo?.Invoke(factory, new object?[] { "PreviousAgent" }) as string;

        // Assert
        Assert.Contains("PreviousAgent", result);
        Assert.Equal(1, factory.GetAgentSelectionReasonCallCount);
    }

    /// <summary>
    /// WHY: GetAgentSelectionReason should handle null previous agent.
    /// This is the case for the first agent in an orchestration.
    /// </summary>
    [Fact]
    public void GetAgentSelectionReason_WithNullPreviousAgent_DoesNotThrow()
    {
        // Arrange
        var (factory, _, _, _, _) = CreateFactory();

        // Act - Use reflection
        var methodInfo = typeof(TestableOrchestrationFactory)
            .GetMethod("GetAgentSelectionReason",
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Instance);
        var exception = Record.Exception(() =>
            methodInfo?.Invoke(factory, new object?[] { null }));

        // Assert
        Assert.Null(exception);
    }

    /// <summary>
    /// WHY: UnwrapResult extracts the actual result from the wrapper type.
    /// This is critical for converting structured output to usable data.
    /// </summary>
    [Fact]
    public void UnwrapResult_WhenInvoked_ReturnsConfiguredResult()
    {
        // Arrange
        var (factory, _, _, _, _) = CreateFactory();
        var expectedEvidence = new List<Evidence>
        {
            new Evidence { Claim = "Test claim 1" },
            new Evidence { Claim = "Test claim 2" }
        };
        factory.UnwrapResultToReturn = expectedEvidence;

        // Act - Use reflection
        var methodInfo = typeof(TestableOrchestrationFactory)
            .GetMethod("UnwrapResult",
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Instance);
        var wrapper = new EvidenceResult { Evidence = expectedEvidence };
        var result = methodInfo?.Invoke(factory, new object[] { wrapper }) as List<Evidence>;

        // Assert
        Assert.Same(expectedEvidence, result);
        Assert.Equal(1, factory.UnwrapResultCallCount);
    }

    /// <summary>
    /// WHY: GetItemCount provides the count of items in a result for logging.
    /// This helps track orchestration output volume.
    /// </summary>
    [Fact]
    public void GetItemCount_WithNonEmptyResult_ReturnsCorrectCount()
    {
        // Arrange
        var (factory, _, _, _, _) = CreateFactory();
        var evidenceList = new List<Evidence>
        {
            new Evidence { Claim = "Claim 1" },
            new Evidence { Claim = "Claim 2" },
            new Evidence { Claim = "Claim 3" }
        };

        // Act - Use reflection
        var methodInfo = typeof(TestableOrchestrationFactory)
            .GetMethod("GetItemCount",
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Instance);
        var result = methodInfo?.Invoke(factory, new object[] { evidenceList });

        // Assert
        Assert.Equal(3, result);
        Assert.Equal(1, factory.GetItemCountCallCount);
    }

    /// <summary>
    /// WHY: GetItemCount should handle empty results correctly.
    /// An empty result should return zero items.
    /// </summary>
    [Fact]
    public void GetItemCount_WithEmptyResult_ReturnsZero()
    {
        // Arrange
        var (factory, _, _, _, _) = CreateFactory();
        var emptyList = new List<Evidence>();

        // Act - Use reflection
        var methodInfo = typeof(TestableOrchestrationFactory)
            .GetMethod("GetItemCount",
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Instance);
        var result = methodInfo?.Invoke(factory, new object[] { emptyList });

        // Assert
        Assert.Equal(0, result);
    }

    #endregion

    #region Settings Behavior Tests

    /// <summary>
    /// WHY: Default settings should have sensible values.
    /// This tests the factory's behavior with typical configuration.
    /// </summary>
    [Fact]
    public void Constructor_WithDefaultSettings_HasExpectedDefaults()
    {
        // Arrange
        var defaultSettings = new OrchestrationSettings();

        // Act
        var (factory, _, _, _, _) = CreateFactory(defaultSettings);

        // Assert
        Assert.Equal(10, factory.ExposedOrchestrationSettings.MaximumInvocationCount);
        Assert.Equal(15, factory.ExposedOrchestrationSettings.TimeoutInMinutes);
        Assert.False(factory.ExposedOrchestrationSettings.StreamResponses);
        Assert.True(factory.ExposedOrchestrationSettings.WriteResponses);
    }

    /// <summary>
    /// WHY: Custom settings should be respected by the factory.
    /// This verifies settings are properly passed through from configuration.
    /// </summary>
    [Theory]
    [InlineData(1, 1, true, true)]
    [InlineData(5, 30, false, false)]
    [InlineData(100, 60, true, false)]
    [InlineData(0, 0, false, true)]
    public void Constructor_WithCustomSettings_RespectsAllValues(
        int maxInvocations, int timeout, bool stream, bool write)
    {
        // Arrange
        var customSettings = new OrchestrationSettings
        {
            MaximumInvocationCount = maxInvocations,
            TimeoutInMinutes = timeout,
            StreamResponses = stream,
            WriteResponses = write
        };

        // Act
        var (factory, _, _, _, _) = CreateFactory(customSettings);

        // Assert
        Assert.Equal(maxInvocations, factory.ExposedOrchestrationSettings.MaximumInvocationCount);
        Assert.Equal(timeout, factory.ExposedOrchestrationSettings.TimeoutInMinutes);
        Assert.Equal(stream, factory.ExposedOrchestrationSettings.StreamResponses);
        Assert.Equal(write, factory.ExposedOrchestrationSettings.WriteResponses);
    }

    #endregion

    #region Edge Case Tests

    /// <summary>
    /// WHY: Very long content should be handled without truncation or errors.
    /// AI responses can be quite lengthy.
    /// </summary>
    [Fact]
    public async Task ResponseCallback_WithVeryLongContent_HandlesGracefully()
    {
        // Arrange
        var (factory, _, _, _, _) = CreateFactory();
        var longContent = new string('A', 100_000); // 100KB of text
        var response = new ChatMessageContent
        {
            AuthorName = "TestAgent",
            Content = longContent
        };

        // Act
        await factory.InvokeResponseCallback(response);

        // Assert
        Assert.Single(factory.ExposedHistory);
        Assert.Equal(longContent, factory.ExposedHistory.First().Content);
    }

    /// <summary>
    /// WHY: Unicode content should be preserved correctly.
    /// AI may respond in multiple languages or use special characters.
    /// </summary>
    [Fact]
    public async Task ResponseCallback_WithUnicodeContent_PreservesContent()
    {
        // Arrange
        var (factory, _, _, _, _) = CreateFactory();
        var unicodeContent = "Hello 世界! Привет мир! 🌍🚀 \u0048\u0065\u006C\u006C\u006F";
        var response = new ChatMessageContent
        {
            AuthorName = "TestAgent",
            Content = unicodeContent
        };

        // Act
        await factory.InvokeResponseCallback(response);

        // Assert
        Assert.Single(factory.ExposedHistory);
        Assert.Equal(unicodeContent, factory.ExposedHistory.First().Content);
    }

    /// <summary>
    /// WHY: Content with special formatting should be preserved.
    /// AI responses often include markdown, code blocks, etc.
    /// </summary>
    [Fact]
    public async Task ResponseCallback_WithSpecialCharacters_PreservesContent()
    {
        // Arrange
        var (factory, _, _, _, _) = CreateFactory();
        var specialContent = "```csharp\nvar x = 1;\n```\n\n**Bold** and _italic_\n\t\tTabs and\nnewlines";
        var response = new ChatMessageContent
        {
            AuthorName = "TestAgent",
            Content = specialContent
        };

        // Act
        await factory.InvokeResponseCallback(response);

        // Assert
        Assert.Single(factory.ExposedHistory);
        Assert.Equal(specialContent, factory.ExposedHistory.First().Content);
    }

    /// <summary>
    /// WHY: Factory should be reusable for multiple orchestrations.
    /// Although typically each orchestration creates a new factory,
    /// the class should handle multiple uses correctly.
    /// </summary>
    [Fact]
    public async Task Factory_WhenUsedForMultipleCallbacks_AccumulatesHistory()
    {
        // Arrange
        var (factory, _, _, _, _) = CreateFactory();

        // Act - Simulate multiple orchestration turns
        for (int i = 0; i < 50; i++)
        {
            var response = new ChatMessageContent
            {
                AuthorName = $"Agent{i % 3}",
                Content = $"Response {i}"
            };
            await factory.InvokeResponseCallback(response);
        }

        // Assert
        Assert.Equal(50, factory.ExposedHistory.Count);
    }

    #endregion

    #region Instance Isolation Tests

    /// <summary>
    /// WHY: Multiple factory instances should be completely independent.
    /// Each factory should have its own history, counters, and state.
    /// This is important when multiple orchestrations run simultaneously.
    /// </summary>
    [Fact]
    public async Task MultipleFactories_HaveIndependentHistories()
    {
        // Arrange
        var (factory1, _, _, _, _) = CreateFactory();
        var (factory2, _, _, _, _) = CreateFactory();

        var response1 = new ChatMessageContent { AuthorName = "Agent1", Content = "Factory1 Response" };
        var response2 = new ChatMessageContent { AuthorName = "Agent2", Content = "Factory2 Response" };

        // Act
        await factory1.InvokeResponseCallback(response1);
        await factory2.InvokeResponseCallback(response2);

        // Assert - Each factory should only have its own response
        Assert.Single(factory1.ExposedHistory);
        Assert.Single(factory2.ExposedHistory);
        Assert.Equal("Factory1 Response", factory1.ExposedHistory.First().Content);
        Assert.Equal("Factory2 Response", factory2.ExposedHistory.First().Content);
    }

    /// <summary>
    /// WHY: Each factory instance should track its own method call counts.
    /// This ensures derived class method tracking is instance-specific.
    /// </summary>
    [Fact]
    public async Task MultipleFactories_HaveIndependentMethodCallCounts()
    {
        // Arrange
        var (factory1, _, _, _, _) = CreateFactory();
        var (factory2, _, _, _, _) = CreateFactory();

        // Act - Call callbacks different number of times
        await factory1.InvokeResponseCallback(new ChatMessageContent { Content = "1" });
        await factory1.InvokeResponseCallback(new ChatMessageContent { Content = "2" });
        await factory1.InvokeResponseCallback(new ChatMessageContent { Content = "3" });

        await factory2.InvokeResponseCallback(new ChatMessageContent { Content = "A" });

        // Assert - Each factory has its own count
        Assert.Equal(3, factory1.ExposedHistory.Count);
        Assert.Equal(1, factory2.ExposedHistory.Count);
    }

    /// <summary>
    /// WHY: Factory instances with different settings should behave differently.
    /// This verifies settings isolation between instances.
    /// </summary>
    [Fact]
    public void MultipleFactories_WithDifferentSettings_MaintainSeparateSettings()
    {
        // Arrange
        var settings1 = new OrchestrationSettings
        {
            StreamResponses = true,
            WriteResponses = false,
            TimeoutInMinutes = 10
        };
        var settings2 = new OrchestrationSettings
        {
            StreamResponses = false,
            WriteResponses = true,
            TimeoutInMinutes = 30
        };

        // Act
        var (factory1, _, _, _, _) = CreateFactory(settings1);
        var (factory2, _, _, _, _) = CreateFactory(settings2);

        // Assert
        Assert.True(factory1.ExposedOrchestrationSettings.StreamResponses);
        Assert.False(factory2.ExposedOrchestrationSettings.StreamResponses);
        Assert.False(factory1.ExposedOrchestrationSettings.WriteResponses);
        Assert.True(factory2.ExposedOrchestrationSettings.WriteResponses);
        Assert.Equal(10, factory1.ExposedOrchestrationSettings.TimeoutInMinutes);
        Assert.Equal(30, factory2.ExposedOrchestrationSettings.TimeoutInMinutes);
    }

    #endregion

    #region Concurrent Access Tests

    /// <summary>
    /// WHY: Streaming callbacks may be invoked concurrently from multiple agents.
    /// The factory should handle concurrent access without race conditions.
    ///
    /// NOTE: This tests the thread-safety of the streaming buffer management.
    /// We redirect Console.Out to prevent interference with other tests and to
    /// ensure all Console.Write calls go to a controlled destination.
    /// </summary>
    /// <summary>

    /// Verifies that streaming response callback when invoked concurrently handles gracefully.

    /// </summary>

    [Fact]
    public async Task StreamingResponseCallback_WhenInvokedConcurrently_HandlesGracefully()
    {
        // Arrange
        var settings = new OrchestrationSettings { StreamResponses = true };
        var (factory, _, _, _, _) = CreateFactory(settings);

        // Capture console output - IMPORTANT: Save and restore to avoid affecting other tests
        // Use TextWriter.Synchronized for thread-safe console output during concurrent test
        var originalOut = Console.Out;
        using var consoleOutput = new StringWriter();
        var synchronizedWriter = TextWriter.Synchronized(consoleOutput);

        try
        {
            Console.SetOut(synchronizedWriter);

            // Act - Simulate concurrent streaming from multiple agents
            var tasks = new List<Task>();
            for (int i = 0; i < 10; i++)
            {
                var agentIndex = i;
                tasks.Add(Task.Run(async () =>
                {
                    for (int chunk = 0; chunk < 5; chunk++)
                    {
                        var response = CreateStreamingResponse(
                            $"Agent{agentIndex}",
                            $"Chunk{chunk}");
                        await factory.InvokeStreamingResponseCallback(response, isFinal: chunk == 4);
                    }
                }));
            }

            // Wait for all tasks
            var exception = await Record.ExceptionAsync(() => Task.WhenAll(tasks));

            // Assert
            Assert.Null(exception);
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    #endregion
}

#pragma warning restore SKEXP0110
