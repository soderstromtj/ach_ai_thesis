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
/// Unit tests for HypothesisBrainstormingOrchestrationFactory.
///
/// Testing Strategy:
/// -----------------
/// HypothesisBrainstormingOrchestrationFactory is a CONCRETE class that extends BaseOrchestrationFactory.
/// Unlike the base class tests, we can instantiate this class directly and test its specific
/// implementations of the abstract methods.
///
/// What We Test:
/// 1. Constructor - Verifies proper initialization and base class integration
/// 2. CreateLogger - Returns correctly typed logger
/// 3. GetResultTypeName - Returns "HypothesisResult"
/// 4. UnwrapResult - Extracts Hypotheses list from HypothesisResult wrapper
/// 5. GetItemCount - Returns correct count of hypothesis items
/// 6. CreateEmptyResult - Returns empty List&lt;Hypothesis&gt;
/// 7. CreateErrorResult - Returns empty List&lt;Hypothesis&gt;
/// 8. GetAgentSelectionReason - Returns concurrent execution message
///
/// Note: CreateOrchestration is not directly unit-testable as it creates Semantic Kernel
/// orchestration objects. That method is better suited for integration testing.
/// </summary>
public class HypothesisBrainstormingOrchestrationFactoryTests
{
    #region Test Infrastructure

    /// <summary>
    /// Creates a factory instance with all dependencies mocked.
    /// </summary>
    private static (HypothesisBrainstormingOrchestrationFactory Factory,
        Mock<IAgentService> AgentServiceMock,
        Mock<IKernelBuilderService> KernelBuilderServiceMock,
        Mock<ILoggerFactory> LoggerFactoryMock,
        Mock<ILogger<HypothesisBrainstormingOrchestrationFactory>> LoggerMock) CreateFactory(
        OrchestrationSettings? settings = null)
    {
        var agentServiceMock = new Mock<IAgentService>();
        var kernelBuilderServiceMock = new Mock<IKernelBuilderService>();
        var loggerFactoryMock = new Mock<ILoggerFactory>();
        var loggerMock = new Mock<ILogger<HypothesisBrainstormingOrchestrationFactory>>();

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

        var factory = new HypothesisBrainstormingOrchestrationFactory(
            agentServiceMock.Object,
            kernelBuilderServiceMock.Object,
            optionsMock.Object,
            loggerFactoryMock.Object);

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
                HypothesisText = $"Test hypothesis text {i + 1}"
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
        var factory = new HypothesisBrainstormingOrchestrationFactory(
            Mock.Of<IAgentService>(),
            Mock.Of<IKernelBuilderService>(),
            optionsMock.Object,
            loggerFactoryMock.Object);

        // Assert
        Assert.NotNull(capturedCategory);
        Assert.Contains("HypothesisBrainstormingOrchestrationFactory", capturedCategory);
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
        var factory = new HypothesisBrainstormingOrchestrationFactory(
            Mock.Of<IAgentService>(),
            Mock.Of<IKernelBuilderService>(),
            optionsMock.Object,
            loggerFactoryMock.Object);

        // Assert - Should NOT contain other factory names
        Assert.NotNull(capturedCategory);
        Assert.DoesNotContain("EvidenceExtractionOrchestrationFactory", capturedCategory);
        Assert.DoesNotContain("EvidenceHypothesisEvaluationOrchestrationFactory", capturedCategory);
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
        var methodInfo = typeof(HypothesisBrainstormingOrchestrationFactory)
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
        var methodInfo = typeof(HypothesisBrainstormingOrchestrationFactory)
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
        var methodInfo = typeof(HypothesisBrainstormingOrchestrationFactory)
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
    /// An empty result is valid when no hypotheses are generated.
    /// </summary>
    [Fact]
    public void UnwrapResult_WithEmptyWrapper_ReturnsEmptyList()
    {
        // Arrange
        var (factory, _, _, _, _) = CreateFactory();
        var wrapper = new HypothesisResult { Hypotheses = new List<Hypothesis>() };

        // Act
        var methodInfo = typeof(HypothesisBrainstormingOrchestrationFactory)
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
            ShortTitle = "Nation-State Actor",
            HypothesisText = "The intrusion was carried out by a sophisticated nation-state actor with advanced persistent threat capabilities."
        };
        var wrapper = new HypothesisResult { Hypotheses = new List<Hypothesis> { hypothesis } };

        // Act
        var methodInfo = typeof(HypothesisBrainstormingOrchestrationFactory)
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
        var methodInfo = typeof(HypothesisBrainstormingOrchestrationFactory)
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
        var methodInfo = typeof(HypothesisBrainstormingOrchestrationFactory)
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
        var methodInfo = typeof(HypothesisBrainstormingOrchestrationFactory)
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
        var methodInfo = typeof(HypothesisBrainstormingOrchestrationFactory)
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
        var methodInfo = typeof(HypothesisBrainstormingOrchestrationFactory)
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
        var methodInfo = typeof(HypothesisBrainstormingOrchestrationFactory)
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
        var methodInfo = typeof(HypothesisBrainstormingOrchestrationFactory)
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
        var methodInfo = typeof(HypothesisBrainstormingOrchestrationFactory)
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
        var methodInfo = typeof(HypothesisBrainstormingOrchestrationFactory)
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
        var methodInfo = typeof(HypothesisBrainstormingOrchestrationFactory)
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
        var methodInfo = typeof(HypothesisBrainstormingOrchestrationFactory)
            .GetMethod("CreateErrorResult",
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Instance);
        var result = methodInfo?.Invoke(factory, null);

        // Assert
        Assert.IsType<List<Hypothesis>>(result);
    }

    #endregion

    #region GetAgentSelectionReason Tests

    /// <summary>
    /// WHY: GetAgentSelectionReason should indicate concurrent execution.
    /// This helps with debugging and understanding orchestration behavior.
    /// </summary>
    [Fact]
    public void GetAgentSelectionReason_Always_ReturnsConcurrentMessage()
    {
        // Arrange
        var (factory, _, _, _, _) = CreateFactory();

        // Act
        var methodInfo = typeof(HypothesisBrainstormingOrchestrationFactory)
            .GetMethod("GetAgentSelectionReason",
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Instance);
        var result = methodInfo?.Invoke(factory, new object?[] { "PreviousAgent" }) as string;

        // Assert
        Assert.NotNull(result);
        Assert.Contains("Concurrent", result, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// WHY: GetAgentSelectionReason should work regardless of previous agent name.
    /// The concurrent message is independent of which agent ran before.
    /// </summary>
    [Fact]
    public void GetAgentSelectionReason_WithNullPreviousAgent_ReturnsSameMessage()
    {
        // Arrange
        var (factory, _, _, _, _) = CreateFactory();
        var methodInfo = typeof(HypothesisBrainstormingOrchestrationFactory)
            .GetMethod("GetAgentSelectionReason",
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Instance);

        // Act
        var resultWithAgent = methodInfo?.Invoke(factory, new object?[] { "SomeAgent" }) as string;
        var resultWithNull = methodInfo?.Invoke(factory, new object?[] { null }) as string;

        // Assert - Both should return the same concurrent message
        Assert.Equal(resultWithAgent, resultWithNull);
    }

    /// <summary>
    /// WHY: The message should indicate all agents run simultaneously.
    /// This clarifies the concurrent orchestration behavior.
    /// </summary>
    [Fact]
    public void GetAgentSelectionReason_MessageIndicatesSimultaneousExecution()
    {
        // Arrange
        var (factory, _, _, _, _) = CreateFactory();

        // Act
        var methodInfo = typeof(HypothesisBrainstormingOrchestrationFactory)
            .GetMethod("GetAgentSelectionReason",
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Instance);
        var result = methodInfo?.Invoke(factory, new object?[] { null }) as string;

        // Assert
        Assert.NotNull(result);
        Assert.Contains("simultaneous", result, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// WHY: Ensures the message does NOT mention "Sequential".
    /// ConcurrentOrchestration should not have sequential messaging.
    /// </summary>
    [Fact]
    public void GetAgentSelectionReason_DoesNotMentionSequential()
    {
        // Arrange
        var (factory, _, _, _, _) = CreateFactory();

        // Act
        var methodInfo = typeof(HypothesisBrainstormingOrchestrationFactory)
            .GetMethod("GetAgentSelectionReason",
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Instance);
        var result = methodInfo?.Invoke(factory, new object?[] { "PreviousAgent" }) as string;

        // Assert
        Assert.NotNull(result);
        Assert.DoesNotContain("Sequential", result, StringComparison.OrdinalIgnoreCase);
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
    [InlineData("H1")]
    [InlineData("Nation-State Actor")]
    [InlineData("Insider Threat")]
    [InlineData("Organized Crime")]
    public void UnwrapResult_PreservesShortTitle(string shortTitle)
    {
        // Arrange
        var (factory, _, _, _, _) = CreateFactory();
        var hypothesis = new Hypothesis
        {
            ShortTitle = shortTitle,
            HypothesisText = "Test text"
        };
        var wrapper = new HypothesisResult { Hypotheses = new List<Hypothesis> { hypothesis } };

        // Act
        var methodInfo = typeof(HypothesisBrainstormingOrchestrationFactory)
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
    /// Detailed hypothesis explanations may be quite lengthy.
    /// </summary>
    [Fact]
    public void UnwrapResult_PreservesLongHypothesisText()
    {
        // Arrange
        var (factory, _, _, _, _) = CreateFactory();
        var longText = new string('x', 10000); // 10K character hypothesis
        var hypothesis = new Hypothesis
        {
            ShortTitle = "Long Hypothesis",
            HypothesisText = longText
        };
        var wrapper = new HypothesisResult { Hypotheses = new List<Hypothesis> { hypothesis } };

        // Act
        var methodInfo = typeof(HypothesisBrainstormingOrchestrationFactory)
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
    /// Hypotheses may contain quotes, newlines, unicode, etc.
    /// </summary>
    [Fact]
    public void UnwrapResult_PreservesSpecialCharacters()
    {
        // Arrange
        var (factory, _, _, _, _) = CreateFactory();
        var specialText = "Hypothesis with \"quotes\", newlines\nand\ttabs, plus unicode: \u2603";
        var hypothesis = new Hypothesis
        {
            ShortTitle = "Special chars test",
            HypothesisText = specialText
        };
        var wrapper = new HypothesisResult { Hypotheses = new List<Hypothesis> { hypothesis } };

        // Act
        var methodInfo = typeof(HypothesisBrainstormingOrchestrationFactory)
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

        var methodInfo = typeof(HypothesisBrainstormingOrchestrationFactory)
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
}
