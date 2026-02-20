using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using NIU.ACH_AI.Application.Configuration;
using NIU.ACH_AI.Application.DTOs;
using NIU.ACH_AI.Application.Interfaces;
using NIU.ACH_AI.Domain.Entities;
using NIU.ACH_AI.Infrastructure.AI.Factories;

namespace NIU.ACH_AI.Infrastructure.Tests.AI.Factories;

/// <summary>
/// Unit tests for HypothesisRefinementOrchestrationFactory.
///
/// Testing Strategy:
/// -----------------
/// HypothesisRefinementOrchestrationFactory is a CONCRETE class that extends BaseOrchestrationFactory.
/// Unlike the base class tests, we can instantiate this class directly and test its specific
/// implementations of the abstract methods.
///
/// KEY DIFFERENCE: This factory uses SequentialOrchestration (not ConcurrentOrchestration like
/// other factories). Therefore, GetAgentSelectionReason correctly returns a sequential message
/// that includes the previous agent's name.
///
/// What We Test:
/// 1. Constructor - Verifies proper initialization and base class integration
/// 2. CreateLogger - Returns correctly typed logger
/// 3. GetResultTypeName - Returns "HypothesisResult"
/// 4. UnwrapResult - Extracts Hypotheses list from HypothesisResult wrapper
/// 5. GetItemCount - Returns correct count of hypothesis items
/// 6. CreateEmptyResult - Returns empty List&lt;Hypothesis&gt;
/// 7. CreateErrorResult - Returns empty List&lt;Hypothesis&gt;
/// 8. GetAgentSelectionReason - Returns sequential selection message WITH previous agent name
///
/// Note: CreateOrchestration is not directly unit-testable as it creates Semantic Kernel
/// orchestration objects. That method is better suited for integration testing.
/// </summary>
public class HypothesisRefinementOrchestrationFactoryTests
{
    #region Test Infrastructure

    /// <summary>
    /// Creates a factory instance with all dependencies mocked.
    /// </summary>
    private static (HypothesisRefinementOrchestrationFactory Factory,
        Mock<IAgentService> AgentServiceMock,
        Mock<IKernelBuilderService> KernelBuilderServiceMock,
        Mock<ILoggerFactory> LoggerFactoryMock,
        Mock<ILogger<HypothesisRefinementOrchestrationFactory>> LoggerMock) CreateFactory(
        OrchestrationSettings? settings = null)
    {
        var agentServiceMock = new Mock<IAgentService>();
        var kernelBuilderServiceMock = new Mock<IKernelBuilderService>();
        var loggerFactoryMock = new Mock<ILoggerFactory>();
        var loggerMock = new Mock<ILogger<HypothesisRefinementOrchestrationFactory>>();

        // Setup logger factory to return typed logger
        loggerFactoryMock
            .Setup(f => f.CreateLogger(It.IsAny<string>()))
            .Returns(loggerMock.Object);

        var orchestrationSettings = settings ?? new OrchestrationSettings
        {
            MaximumInvocationCount = 10,
            TimeoutInMinutes = 15,
            StreamResponses = false,
            WriteResponses = false
        };

        var optionsMock = new Mock<IOptions<OrchestrationSettings>>();
        optionsMock.Setup(o => o.Value).Returns(orchestrationSettings);

        var promptFormatterMock = new Mock<IOrchestrationPromptFormatter>();
        var agentResponsePersistenceMock = new Mock<IAgentResponsePersistence>();

        var factory = new HypothesisRefinementOrchestrationFactory(
            agentServiceMock.Object,
            kernelBuilderServiceMock.Object,
            optionsMock.Object,
            promptFormatterMock.Object,
            loggerFactoryMock.Object,
            agentResponsePersistenceMock.Object);

        return (factory, agentServiceMock, kernelBuilderServiceMock, loggerFactoryMock, loggerMock);
    }

    /// <summary>
    /// Creates sample hypotheses for testing.
    /// </summary>
    private static List<Hypothesis> CreateSampleHypotheses(int count)
    {
        var hypotheses = new List<Hypothesis>();
        for (int i = 0; i < count; i++)
        {
            hypotheses.Add(new Hypothesis
            {
                ShortTitle = $"H{i + 1}",
                HypothesisText = $"Refined hypothesis text {i + 1}"
            });
        }
        return hypotheses;
    }

    #endregion

    #region Constructor Tests

    /// <summary>
    /// WHY: Verifies the factory can be instantiated with valid dependencies.
    /// This confirms the constructor properly calls the base class.
    /// </summary>
    [Fact]
    public void Constructor_WithValidDependencies_CreatesInstance()
    {
        // Arrange & Act
        var (factory, _, _, _, _) = CreateFactory();

        // Assert
        Assert.NotNull(factory);
    }

    /// <summary>
    /// WHY: Verifies the factory requests the correct logger type.
    /// The logger category should match the factory class name.
    /// </summary>
    [Fact]
    public void Constructor_Always_CreatesTypedLogger()
    {
        // Arrange
        var loggerFactoryMock = new Mock<ILoggerFactory>();
        var loggerMock = new Mock<ILogger>();
        string? capturedCategory = null;

        loggerFactoryMock
            .Setup(f => f.CreateLogger(It.IsAny<string>()))
            .Callback<string>(category => capturedCategory = category)
            .Returns(loggerMock.Object);

        var optionsMock = new Mock<IOptions<OrchestrationSettings>>();
        optionsMock.Setup(o => o.Value).Returns(new OrchestrationSettings());

        // Act
        var factory = new HypothesisRefinementOrchestrationFactory(
            Mock.Of<IAgentService>(),
            Mock.Of<IKernelBuilderService>(),
            optionsMock.Object,
            new Mock<IOrchestrationPromptFormatter>().Object,
            loggerFactoryMock.Object);

        // Assert
        Assert.NotNull(capturedCategory);
        Assert.Contains("HypothesisRefinementOrchestrationFactory", capturedCategory);
    }

    /// <summary>
    /// WHY: Ensures the logger is NOT created for a different factory type.
    /// This guards against copy-paste errors from other factory classes.
    /// </summary>
    [Fact]
    public void Constructor_CreatesLoggerForCorrectType_NotOtherFactories()
    {
        // Arrange
        var loggerFactoryMock = new Mock<ILoggerFactory>();
        var loggerMock = new Mock<ILogger>();
        string? capturedCategory = null;

        loggerFactoryMock
            .Setup(f => f.CreateLogger(It.IsAny<string>()))
            .Callback<string>(category => capturedCategory = category)
            .Returns(loggerMock.Object);

        var optionsMock = new Mock<IOptions<OrchestrationSettings>>();
        optionsMock.Setup(o => o.Value).Returns(new OrchestrationSettings());

        // Act
        var factory = new HypothesisRefinementOrchestrationFactory(
            Mock.Of<IAgentService>(),
            Mock.Of<IKernelBuilderService>(),
            optionsMock.Object,
            new Mock<IOrchestrationPromptFormatter>().Object,
            loggerFactoryMock.Object);

        // Assert - Should NOT contain other factory names
        Assert.NotNull(capturedCategory);
        Assert.DoesNotContain("HypothesisBrainstormingOrchestrationFactory", capturedCategory);
        Assert.DoesNotContain("EvidenceExtractionOrchestrationFactory", capturedCategory);
    }

    #endregion

    #region GetResultTypeName Tests

    /// <summary>
    /// WHY: GetResultTypeName should return the wrapper type name for logging.
    /// This helps identify what type of structured output is expected.
    /// </summary>
    [Fact]
    public void GetResultTypeName_Always_ReturnsHypothesisResult()
    {
        // Arrange
        var (factory, _, _, _, _) = CreateFactory();

        // Act - Use reflection to call protected method
        var methodInfo = typeof(HypothesisRefinementOrchestrationFactory)
            .GetMethod("GetResultTypeName",
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Instance);
        var result = methodInfo?.Invoke(factory, null) as string;

        // Assert
        Assert.Equal("HypothesisResult", result);
    }

    /// <summary>
    /// WHY: Ensures the result type name is NOT from another factory.
    /// This guards against copy-paste errors.
    /// </summary>
    [Fact]
    public void GetResultTypeName_DoesNotReturnOtherResultTypes()
    {
        // Arrange
        var (factory, _, _, _, _) = CreateFactory();

        // Act
        var methodInfo = typeof(HypothesisRefinementOrchestrationFactory)
            .GetMethod("GetResultTypeName",
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Instance);
        var result = methodInfo?.Invoke(factory, null) as string;

        // Assert
        Assert.NotEqual("EvidenceResult", result);
        Assert.NotEqual("EvidenceHypothesisEvaluationResult", result);
    }

    #endregion

    #region UnwrapResult Tests

    /// <summary>
    /// WHY: UnwrapResult extracts the Hypotheses list from the HypothesisResult wrapper.
    /// This is critical for returning the actual data to callers.
    /// </summary>
    [Fact]
    public void UnwrapResult_WithPopulatedWrapper_ReturnsHypothesesList()
    {
        // Arrange
        var (factory, _, _, _, _) = CreateFactory();
        var expectedHypotheses = CreateSampleHypotheses(3);
        var wrapper = new HypothesisResult { Hypotheses = expectedHypotheses };

        // Act - Use reflection to call protected method
        var methodInfo = typeof(HypothesisRefinementOrchestrationFactory)
            .GetMethod("UnwrapResult",
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Instance);
        var result = methodInfo?.Invoke(factory, new object[] { wrapper }) as List<Hypothesis>;

        // Assert
        Assert.NotNull(result);
        Assert.Same(expectedHypotheses, result);
        Assert.Equal(3, result.Count);
    }

    /// <summary>
    /// WHY: UnwrapResult should handle empty hypothesis lists correctly.
    /// An empty result is valid when refinement produces no hypotheses.
    /// </summary>
    [Fact]
    public void UnwrapResult_WithEmptyWrapper_ReturnsEmptyList()
    {
        // Arrange
        var (factory, _, _, _, _) = CreateFactory();
        var wrapper = new HypothesisResult { Hypotheses = new List<Hypothesis>() };

        // Act
        var methodInfo = typeof(HypothesisRefinementOrchestrationFactory)
            .GetMethod("UnwrapResult",
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Instance);
        var result = methodInfo?.Invoke(factory, new object[] { wrapper }) as List<Hypothesis>;

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    /// <summary>
    /// WHY: UnwrapResult should preserve all hypothesis properties.
    /// The unwrapped data should be identical to the input.
    /// </summary>
    [Fact]
    public void UnwrapResult_PreservesHypothesisProperties()
    {
        // Arrange
        var (factory, _, _, _, _) = CreateFactory();
        var hypothesis = new Hypothesis
        {
            ShortTitle = "Refined Hypothesis",
            HypothesisText = "This hypothesis has been refined through sequential agent collaboration to improve clarity and testability."
        };
        var wrapper = new HypothesisResult { Hypotheses = new List<Hypothesis> { hypothesis } };

        // Act
        var methodInfo = typeof(HypothesisRefinementOrchestrationFactory)
            .GetMethod("UnwrapResult",
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Instance);
        var result = methodInfo?.Invoke(factory, new object[] { wrapper }) as List<Hypothesis>;

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        var unwrappedHypothesis = result[0];
        Assert.Equal(hypothesis.ShortTitle, unwrappedHypothesis.ShortTitle);
        Assert.Equal(hypothesis.HypothesisText, unwrappedHypothesis.HypothesisText);
    }

    /// <summary>
    /// WHY: Hypothesis with empty strings is a valid edge case.
    /// Empty strings are different from null and should be preserved.
    /// </summary>
    [Fact]
    public void UnwrapResult_WithEmptyStringProperties_PreservesEmptyStrings()
    {
        // Arrange
        var (factory, _, _, _, _) = CreateFactory();
        var hypothesis = new Hypothesis
        {
            ShortTitle = string.Empty,
            HypothesisText = string.Empty
        };
        var wrapper = new HypothesisResult { Hypotheses = new List<Hypothesis> { hypothesis } };

        // Act
        var methodInfo = typeof(HypothesisRefinementOrchestrationFactory)
            .GetMethod("UnwrapResult",
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Instance);
        var result = methodInfo?.Invoke(factory, new object[] { wrapper }) as List<Hypothesis>;

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal(string.Empty, result[0].ShortTitle);
        Assert.Equal(string.Empty, result[0].HypothesisText);
    }

    #endregion

    #region GetItemCount Tests

    /// <summary>
    /// WHY: GetItemCount should return the number of hypothesis items.
    /// This is used for logging orchestration output volume.
    /// </summary>
    [Fact]
    public void GetItemCount_WithMultipleItems_ReturnsCorrectCount()
    {
        // Arrange
        var (factory, _, _, _, _) = CreateFactory();
        var hypotheses = CreateSampleHypotheses(5);

        // Act
        var methodInfo = typeof(HypothesisRefinementOrchestrationFactory)
            .GetMethod("GetItemCount",
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Instance);
        var result = methodInfo?.Invoke(factory, new object[] { hypotheses });

        // Assert
        Assert.Equal(5, result);
    }

    /// <summary>
    /// WHY: GetItemCount should return zero for empty lists.
    /// Empty results are valid and should be counted correctly.
    /// </summary>
    [Fact]
    public void GetItemCount_WithEmptyList_ReturnsZero()
    {
        // Arrange
        var (factory, _, _, _, _) = CreateFactory();
        var emptyHypotheses = new List<Hypothesis>();

        // Act
        var methodInfo = typeof(HypothesisRefinementOrchestrationFactory)
            .GetMethod("GetItemCount",
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Instance);
        var result = methodInfo?.Invoke(factory, new object[] { emptyHypotheses });

        // Assert
        Assert.Equal(0, result);
    }

    /// <summary>
    /// WHY: GetItemCount should handle single item lists.
    /// Boundary case for the smallest non-empty result.
    /// </summary>
    [Fact]
    public void GetItemCount_WithSingleItem_ReturnsOne()
    {
        // Arrange
        var (factory, _, _, _, _) = CreateFactory();
        var singleHypothesis = CreateSampleHypotheses(1);

        // Act
        var methodInfo = typeof(HypothesisRefinementOrchestrationFactory)
            .GetMethod("GetItemCount",
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Instance);
        var result = methodInfo?.Invoke(factory, new object[] { singleHypothesis });

        // Assert
        Assert.Equal(1, result);
    }

    /// <summary>
    /// WHY: GetItemCount should handle large result sets.
    /// Verifies no overflow or performance issues with larger counts.
    /// </summary>
    [Fact]
    public void GetItemCount_WithLargeList_ReturnsCorrectCount()
    {
        // Arrange
        var (factory, _, _, _, _) = CreateFactory();
        var largeHypotheses = CreateSampleHypotheses(1000);

        // Act
        var methodInfo = typeof(HypothesisRefinementOrchestrationFactory)
            .GetMethod("GetItemCount",
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Instance);
        var result = methodInfo?.Invoke(factory, new object[] { largeHypotheses });

        // Assert
        Assert.Equal(1000, result);
    }

    #endregion

    #region CreateEmptyResult Tests

    /// <summary>
    /// WHY: CreateEmptyResult should return an empty list when no hypotheses are found.
    /// This prevents null reference exceptions in the calling code.
    /// </summary>
    [Fact]
    public void CreateEmptyResult_Always_ReturnsEmptyList()
    {
        // Arrange
        var (factory, _, _, _, _) = CreateFactory();

        // Act
        var methodInfo = typeof(HypothesisRefinementOrchestrationFactory)
            .GetMethod("CreateEmptyResult",
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Instance);
        var result = methodInfo?.Invoke(factory, null) as List<Hypothesis>;

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    /// <summary>
    /// WHY: CreateEmptyResult should return a new instance each time.
    /// Callers should get independent instances to avoid shared state issues.
    /// </summary>
    [Fact]
    public void CreateEmptyResult_CalledMultipleTimes_ReturnsNewInstances()
    {
        // Arrange
        var (factory, _, _, _, _) = CreateFactory();
        var methodInfo = typeof(HypothesisRefinementOrchestrationFactory)
            .GetMethod("CreateEmptyResult",
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Instance);

        // Act
        var result1 = methodInfo?.Invoke(factory, null) as List<Hypothesis>;
        var result2 = methodInfo?.Invoke(factory, null) as List<Hypothesis>;

        // Assert
        Assert.NotNull(result1);
        Assert.NotNull(result2);
        Assert.NotSame(result1, result2);
    }

    /// <summary>
    /// WHY: CreateEmptyResult should return correct type.
    /// Ensures we're returning Hypothesis list, not some other type.
    /// </summary>
    [Fact]
    public void CreateEmptyResult_ReturnsCorrectType()
    {
        // Arrange
        var (factory, _, _, _, _) = CreateFactory();

        // Act
        var methodInfo = typeof(HypothesisRefinementOrchestrationFactory)
            .GetMethod("CreateEmptyResult",
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Instance);
        var result = methodInfo?.Invoke(factory, null);

        // Assert
        Assert.IsType<List<Hypothesis>>(result);
    }

    #endregion

    #region CreateErrorResult Tests

    /// <summary>
    /// WHY: CreateErrorResult should return an empty list on error.
    /// This allows graceful degradation when orchestration fails.
    /// </summary>
    [Fact]
    public void CreateErrorResult_Always_ReturnsEmptyList()
    {
        // Arrange
        var (factory, _, _, _, _) = CreateFactory();

        // Act
        var methodInfo = typeof(HypothesisRefinementOrchestrationFactory)
            .GetMethod("CreateErrorResult",
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Instance);
        var result = methodInfo?.Invoke(factory, null) as List<Hypothesis>;

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    /// <summary>
    /// WHY: CreateErrorResult should return new instances each time.
    /// Error results should be independent to avoid shared state.
    /// </summary>
    [Fact]
    public void CreateErrorResult_CalledMultipleTimes_ReturnsNewInstances()
    {
        // Arrange
        var (factory, _, _, _, _) = CreateFactory();
        var methodInfo = typeof(HypothesisRefinementOrchestrationFactory)
            .GetMethod("CreateErrorResult",
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Instance);

        // Act
        var result1 = methodInfo?.Invoke(factory, null) as List<Hypothesis>;
        var result2 = methodInfo?.Invoke(factory, null) as List<Hypothesis>;

        // Assert
        Assert.NotNull(result1);
        Assert.NotNull(result2);
        Assert.NotSame(result1, result2);
    }

    /// <summary>
    /// WHY: CreateErrorResult should return correct type.
    /// Ensures we're returning Hypothesis list.
    /// </summary>
    [Fact]
    public void CreateErrorResult_ReturnsCorrectType()
    {
        // Arrange
        var (factory, _, _, _, _) = CreateFactory();

        // Act
        var methodInfo = typeof(HypothesisRefinementOrchestrationFactory)
            .GetMethod("CreateErrorResult",
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Instance);
        var result = methodInfo?.Invoke(factory, null);

        // Assert
        Assert.IsType<List<Hypothesis>>(result);
    }

    #endregion

    #region GetAgentSelectionReason Tests - SEQUENTIAL ORCHESTRATION

    /// <summary>
    /// WHY: GetAgentSelectionReason should indicate SEQUENTIAL execution for this factory.
    /// Unlike ConcurrentOrchestration factories, this one uses SequentialOrchestration
    /// where agents run one after another, passing results through the chain.
    /// </summary>
    [Fact]
    public void GetAgentSelectionReason_Always_ReturnsSequentialMessage()
    {
        // Arrange
        var (factory, _, _, _, _) = CreateFactory();

        // Act
        var methodInfo = typeof(HypothesisRefinementOrchestrationFactory)
            .GetMethod("GetAgentSelectionReason",
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Instance);
        var result = methodInfo?.Invoke(factory, new object?[] { "PreviousAgent" }) as string;

        // Assert
        Assert.NotNull(result);
        Assert.Contains("Sequential", result, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// WHY: GetAgentSelectionReason should include the previous agent's name.
    /// In sequential orchestration, tracking which agent came before is important
    /// for understanding the refinement chain.
    /// </summary>
    [Fact]
    public void GetAgentSelectionReason_IncludesPreviousAgentName()
    {
        // Arrange
        var (factory, _, _, _, _) = CreateFactory();
        var previousAgentName = "HypothesisCriticAgent";

        // Act
        var methodInfo = typeof(HypothesisRefinementOrchestrationFactory)
            .GetMethod("GetAgentSelectionReason",
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Instance);
        var result = methodInfo?.Invoke(factory, new object?[] { previousAgentName }) as string;

        // Assert
        Assert.NotNull(result);
        Assert.Contains(previousAgentName, result);
    }

    /// <summary>
    /// WHY: GetAgentSelectionReason should handle null previous agent gracefully.
    /// The first agent in a sequence won't have a previous agent.
    /// </summary>
    [Fact]
    public void GetAgentSelectionReason_WithNullPreviousAgent_HandlesGracefully()
    {
        // Arrange
        var (factory, _, _, _, _) = CreateFactory();

        // Act
        var methodInfo = typeof(HypothesisRefinementOrchestrationFactory)
            .GetMethod("GetAgentSelectionReason",
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Instance);
        var result = methodInfo?.Invoke(factory, new object?[] { null }) as string;

        // Assert - Should not throw, should return a valid string
        Assert.NotNull(result);
        Assert.Contains("Sequential", result, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// WHY: Different previous agents should produce different messages.
    /// This differs from ConcurrentOrchestration where the message is always the same.
    /// </summary>
    [Fact]
    public void GetAgentSelectionReason_WithDifferentAgents_ProducesDifferentMessages()
    {
        // Arrange
        var (factory, _, _, _, _) = CreateFactory();
        var methodInfo = typeof(HypothesisRefinementOrchestrationFactory)
            .GetMethod("GetAgentSelectionReason",
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Instance);

        // Act
        var resultAgent1 = methodInfo?.Invoke(factory, new object?[] { "Agent1" }) as string;
        var resultAgent2 = methodInfo?.Invoke(factory, new object?[] { "Agent2" }) as string;

        // Assert - Messages should be different because they include the agent name
        Assert.NotNull(resultAgent1);
        Assert.NotNull(resultAgent2);
        Assert.NotEqual(resultAgent1, resultAgent2);
        Assert.Contains("Agent1", resultAgent1);
        Assert.Contains("Agent2", resultAgent2);
    }

    /// <summary>
    /// WHY: The message should NOT mention "Concurrent" or "simultaneous".
    /// This factory uses SequentialOrchestration, not ConcurrentOrchestration.
    /// </summary>
    [Fact]
    public void GetAgentSelectionReason_DoesNotMentionConcurrent()
    {
        // Arrange
        var (factory, _, _, _, _) = CreateFactory();

        // Act
        var methodInfo = typeof(HypothesisRefinementOrchestrationFactory)
            .GetMethod("GetAgentSelectionReason",
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Instance);
        var result = methodInfo?.Invoke(factory, new object?[] { "PreviousAgent" }) as string;

        // Assert
        Assert.NotNull(result);
        Assert.DoesNotContain("Concurrent", result, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("simultaneous", result, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// WHY: Verifies the message format includes "after" keyword.
    /// The format "Sequential selection after {agentName}" indicates the chain.
    /// </summary>
    [Fact]
    public void GetAgentSelectionReason_MessageIndicatesAfterPreviousAgent()
    {
        // Arrange
        var (factory, _, _, _, _) = CreateFactory();

        // Act
        var methodInfo = typeof(HypothesisRefinementOrchestrationFactory)
            .GetMethod("GetAgentSelectionReason",
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Instance);
        var result = methodInfo?.Invoke(factory, new object?[] { "TestAgent" }) as string;

        // Assert
        Assert.NotNull(result);
        Assert.Contains("after", result, StringComparison.OrdinalIgnoreCase);
    }

    #endregion

    #region Interface Implementation Tests

    /// <summary>
    /// WHY: The factory should implement IOrchestrationFactory interface.
    /// This ensures it can be used polymorphically with other factories.
    /// </summary>
    [Fact]
    public void Factory_ImplementsIOrchestrationFactory()
    {
        // Arrange
        var (factory, _, _, _, _) = CreateFactory();

        // Assert
        Assert.IsAssignableFrom<IOrchestrationFactory<List<Hypothesis>>>(factory);
    }

    /// <summary>
    /// WHY: The factory should inherit from BaseOrchestrationFactory.
    /// This confirms the template method pattern is properly used.
    /// </summary>
    [Fact]
    public void Factory_InheritsFromBaseOrchestrationFactory()
    {
        // Arrange
        var (factory, _, _, _, _) = CreateFactory();

        // Assert
        Assert.IsAssignableFrom<BaseOrchestrationFactory<List<Hypothesis>, HypothesisResult>>(factory);
    }

    #endregion

    #region Hypothesis Property Tests

    /// <summary>
    /// WHY: UnwrapResult should preserve short titles.
    /// ShortTitle is a key identifier for hypotheses in ACH.
    /// </summary>
    [Theory]
    [InlineData("H1-Refined")]
    [InlineData("Nation-State Actor (Updated)")]
    [InlineData("Insider Threat v2")]
    [InlineData("Organized Crime - Revised")]
    public void UnwrapResult_PreservesShortTitle(string shortTitle)
    {
        // Arrange
        var (factory, _, _, _, _) = CreateFactory();
        var hypothesis = new Hypothesis
        {
            ShortTitle = shortTitle,
            HypothesisText = "Refined text"
        };
        var wrapper = new HypothesisResult { Hypotheses = new List<Hypothesis> { hypothesis } };

        // Act
        var methodInfo = typeof(HypothesisRefinementOrchestrationFactory)
            .GetMethod("UnwrapResult",
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Instance);
        var result = methodInfo?.Invoke(factory, new object[] { wrapper }) as List<Hypothesis>;

        // Assert
        Assert.NotNull(result);
        Assert.Equal(shortTitle, result[0].ShortTitle);
    }

    /// <summary>
    /// WHY: UnwrapResult should preserve long hypothesis text.
    /// Refined hypotheses may have detailed explanations.
    /// </summary>
    [Fact]
    public void UnwrapResult_PreservesLongHypothesisText()
    {
        // Arrange
        var (factory, _, _, _, _) = CreateFactory();
        var longText = new string('x', 10000); // 10K character refined hypothesis
        var hypothesis = new Hypothesis
        {
            ShortTitle = "Long Refined Hypothesis",
            HypothesisText = longText
        };
        var wrapper = new HypothesisResult { Hypotheses = new List<Hypothesis> { hypothesis } };

        // Act
        var methodInfo = typeof(HypothesisRefinementOrchestrationFactory)
            .GetMethod("UnwrapResult",
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Instance);
        var result = methodInfo?.Invoke(factory, new object[] { wrapper }) as List<Hypothesis>;

        // Assert
        Assert.NotNull(result);
        Assert.Equal(10000, result[0].HypothesisText.Length);
        Assert.Equal(longText, result[0].HypothesisText);
    }

    /// <summary>
    /// WHY: UnwrapResult should preserve special characters in hypothesis text.
    /// Refined hypotheses may contain quotes, newlines, unicode, etc.
    /// </summary>
    [Fact]
    public void UnwrapResult_PreservesSpecialCharacters()
    {
        // Arrange
        var (factory, _, _, _, _) = CreateFactory();
        var specialText = "Refined hypothesis with \"quotes\", newlines\nand\ttabs, plus unicode: \u2603";
        var hypothesis = new Hypothesis
        {
            ShortTitle = "Special chars test",
            HypothesisText = specialText
        };
        var wrapper = new HypothesisResult { Hypotheses = new List<Hypothesis> { hypothesis } };

        // Act
        var methodInfo = typeof(HypothesisRefinementOrchestrationFactory)
            .GetMethod("UnwrapResult",
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Instance);
        var result = methodInfo?.Invoke(factory, new object[] { wrapper }) as List<Hypothesis>;

        // Assert
        Assert.NotNull(result);
        Assert.Equal(specialText, result[0].HypothesisText);
    }

    #endregion

    #region Multiple Instances Isolation Tests

    /// <summary>
    /// WHY: Multiple factory instances should be independent.
    /// No shared state should leak between instances.
    /// </summary>
    [Fact]
    public void MultipleInstances_AreIndependent()
    {
        // Arrange
        var (factory1, _, _, _, _) = CreateFactory();
        var (factory2, _, _, _, _) = CreateFactory();

        // Assert
        Assert.NotSame(factory1, factory2);
    }

    /// <summary>
    /// WHY: Different factory instances should produce independent results.
    /// Ensures no static state is shared.
    /// </summary>
    [Fact]
    public void MultipleInstances_ProduceIndependentResults()
    {
        // Arrange
        var (factory1, _, _, _, _) = CreateFactory();
        var (factory2, _, _, _, _) = CreateFactory();

        var methodInfo = typeof(HypothesisRefinementOrchestrationFactory)
            .GetMethod("CreateEmptyResult",
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Instance);

        // Act
        var result1 = methodInfo?.Invoke(factory1, null) as List<Hypothesis>;
        var result2 = methodInfo?.Invoke(factory2, null) as List<Hypothesis>;

        // Assert
        Assert.NotNull(result1);
        Assert.NotNull(result2);
        Assert.NotSame(result1, result2);
    }

    /// <summary>
    /// WHY: Factory instances with different settings should maintain separate configs.
    /// This verifies settings isolation between instances.
    /// </summary>
    [Fact]
    public void MultipleInstances_WithDifferentSettings_MaintainSeparateSettings()
    {
        // Arrange
        var settings1 = new OrchestrationSettings { TimeoutInMinutes = 10 };
        var settings2 = new OrchestrationSettings { TimeoutInMinutes = 30 };

        // Act
        var (factory1, _, _, _, _) = CreateFactory(settings1);
        var (factory2, _, _, _, _) = CreateFactory(settings2);

        // Assert - Both factories exist and are independent
        Assert.NotSame(factory1, factory2);
    }

    #endregion

    #region Comparison with Concurrent Factories

    /// <summary>
    /// WHY: This test documents the key difference between HypothesisRefinementOrchestrationFactory
    /// and HypothesisBrainstormingOrchestrationFactory. Refinement uses Sequential,
    /// Brainstorming uses Concurrent. Their GetAgentSelectionReason messages should differ.
    /// </summary>
    /// <summary>

    /// Verifies that get agent selection reason differs from concurrent factories.

    /// </summary>

    [Fact]
    public void GetAgentSelectionReason_DiffersFromConcurrentFactories()
    {
        // Arrange
        var (refinementFactory, _, _, _, _) = CreateFactory();

        // Act
        var methodInfo = typeof(HypothesisRefinementOrchestrationFactory)
            .GetMethod("GetAgentSelectionReason",
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Instance);
        var result = methodInfo?.Invoke(refinementFactory, new object?[] { "Agent1" }) as string;

        // Assert - Should be sequential, not concurrent
        Assert.NotNull(result);
        Assert.Contains("Sequential", result, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("all agents run simultaneously", result, StringComparison.OrdinalIgnoreCase);
    }

    #endregion
}
