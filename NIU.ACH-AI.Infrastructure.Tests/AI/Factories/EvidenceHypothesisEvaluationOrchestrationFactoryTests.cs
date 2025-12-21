using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using NIU.ACH_AI.Application.Configuration;
using NIU.ACH_AI.Application.DTOs;
using NIU.ACH_AI.Application.Interfaces;
using NIU.ACH_AI.Domain.Entities;
using NIU.ACH_AI.Domain.ValueObjects;
using NIU.ACH_AI.Infrastructure.AI.Factories;

namespace NIU.ACH_AI.Infrastructure.Tests.AI.Factories;

/// <summary>
/// Unit tests for EvidenceHypothesisEvaluationOrchestrationFactory.
///
/// Testing Strategy:
/// -----------------
/// EvidenceHypothesisEvaluationOrchestrationFactory is a CONCRETE class that extends BaseOrchestrationFactory.
/// Unlike the base class tests, we can instantiate this class directly and test its specific
/// implementations of the abstract methods.
///
/// What We Test:
/// 1. Constructor - Verifies proper initialization and base class integration
/// 2. CreateLogger - Returns correctly typed logger
/// 3. GetResultTypeName - Returns "EvidenceHypothesisEvaluationResult"
/// 4. UnwrapResult - Extracts Evaluations list from EvidenceHypothesisEvaluationResult wrapper
/// 5. GetItemCount - Returns correct count of evaluation items
/// 6. CreateEmptyResult - Returns empty List&lt;EvidenceHypothesisEvaluation&gt;
/// 7. CreateErrorResult - Returns empty List&lt;EvidenceHypothesisEvaluation&gt;
/// 8. GetAgentSelectionReason - Returns concurrent execution message
///
/// Note: CreateOrchestration is not directly unit-testable as it creates Semantic Kernel
/// orchestration objects. That method is better suited for integration testing.
/// </summary>
public class EvidenceHypothesisEvaluationOrchestrationFactoryTests
{
    #region Test Infrastructure

    /// <summary>
    /// Creates a factory instance with all dependencies mocked.
    /// </summary>
    private static (EvidenceHypothesisEvaluationOrchestrationFactory Factory,
        Mock<IAgentService> AgentServiceMock,
        Mock<IKernelBuilderService> KernelBuilderServiceMock,
        Mock<ILoggerFactory> LoggerFactoryMock,
        Mock<ILogger<EvidenceHypothesisEvaluationOrchestrationFactory>> LoggerMock) CreateFactory(
        OrchestrationSettings? settings = null)
    {
        var agentServiceMock = new Mock<IAgentService>();
        var kernelBuilderServiceMock = new Mock<IKernelBuilderService>();
        var loggerFactoryMock = new Mock<ILoggerFactory>();
        var loggerMock = new Mock<ILogger<EvidenceHypothesisEvaluationOrchestrationFactory>>();

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

        var factory = new EvidenceHypothesisEvaluationOrchestrationFactory(
            agentServiceMock.Object,
            kernelBuilderServiceMock.Object,
            optionsMock.Object,
            loggerFactoryMock.Object);

        return (factory, agentServiceMock, kernelBuilderServiceMock, loggerFactoryMock, loggerMock);
    }

    /// <summary>
    /// Creates sample evaluations for testing.
    /// </summary>
    private static List<EvidenceHypothesisEvaluation> CreateSampleEvaluations(int count)
    {
        var evaluations = new List<EvidenceHypothesisEvaluation>();
        var scores = new[] { EvaluationScore.Consistent, EvaluationScore.Inconsistent, EvaluationScore.Neutral };

        for (int i = 0; i < count; i++)
        {
            evaluations.Add(new EvidenceHypothesisEvaluation
            {
                Hypothesis = new Hypothesis
                {
                    ShortTitle = $"Hypothesis {i + 1}",
                    HypothesisText = $"Test hypothesis text {i + 1}"
                },
                Evidence = new Evidence
                {
                    EvidenceId = Guid.NewGuid(),
                    Claim = $"Test claim {i + 1}",
                    ReferenceSnippet = $"Reference for claim {i + 1}",
                    Type = i % 2 == 0 ? EvidenceType.Fact : EvidenceType.Assumption,
                    Notes = $"Notes for evidence {i + 1}"
                },
                Score = scores[i % 3],
                ScoreRationale = $"Score rationale for evaluation {i + 1}",
                ConfidenceLevel = 0.5m + (i * 0.1m),
                ConfidenceRationale = $"Confidence rationale for evaluation {i + 1}"
            });
        }
        return evaluations;
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
        var factory = new EvidenceHypothesisEvaluationOrchestrationFactory(
            Mock.Of<IAgentService>(),
            Mock.Of<IKernelBuilderService>(),
            optionsMock.Object,
            loggerFactoryMock.Object);

        // Assert
        Assert.NotNull(capturedCategory);
        Assert.Contains("EvidenceHypothesisEvaluationOrchestrationFactory", capturedCategory);
    }

    /// <summary>
    /// WHY: Ensures the logger is NOT created for the wrong factory type.
    /// This guards against copy-paste errors from other factory classes.
    /// </summary>
    [Fact]
    public void Constructor_CreatesLoggerForCorrectType_NotEvidenceExtractionFactory()
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
        var factory = new EvidenceHypothesisEvaluationOrchestrationFactory(
            Mock.Of<IAgentService>(),
            Mock.Of<IKernelBuilderService>(),
            optionsMock.Object,
            loggerFactoryMock.Object);

        // Assert - Should NOT contain EvidenceExtractionOrchestrationFactory
        Assert.NotNull(capturedCategory);
        Assert.DoesNotContain("EvidenceExtractionOrchestrationFactory", capturedCategory);
    }

    #endregion

    #region GetResultTypeName Tests

    /// <summary>
    /// WHY: GetResultTypeName should return the wrapper type name for logging.
    /// This helps identify what type of structured output is expected.
    /// </summary>
    [Fact]
    public void GetResultTypeName_Always_ReturnsEvidenceHypothesisEvaluationResult()
    {
        // Arrange
        var (factory, _, _, _, _) = CreateFactory();

        // Act - Use reflection to call protected method
        var methodInfo = typeof(EvidenceHypothesisEvaluationOrchestrationFactory)
            .GetMethod("GetResultTypeName",
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Instance);
        var result = methodInfo?.Invoke(factory, null) as string;

        // Assert
        Assert.Equal("EvidenceHypothesisEvaluationResult", result);
    }

    /// <summary>
    /// WHY: Ensures the result type name is NOT "EvidenceResult".
    /// This guards against copy-paste errors from EvidenceExtractionOrchestrationFactory.
    /// </summary>
    [Fact]
    public void GetResultTypeName_DoesNotReturnEvidenceResult()
    {
        // Arrange
        var (factory, _, _, _, _) = CreateFactory();

        // Act
        var methodInfo = typeof(EvidenceHypothesisEvaluationOrchestrationFactory)
            .GetMethod("GetResultTypeName",
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Instance);
        var result = methodInfo?.Invoke(factory, null) as string;

        // Assert
        Assert.NotEqual("EvidenceResult", result);
    }

    #endregion

    #region UnwrapResult Tests

    /// <summary>
    /// WHY: UnwrapResult extracts the Evaluations list from the EvidenceHypothesisEvaluationResult wrapper.
    /// This is critical for returning the actual data to callers.
    /// </summary>
    [Fact]
    public void UnwrapResult_WithPopulatedWrapper_ReturnsEvaluationsList()
    {
        // Arrange
        var (factory, _, _, _, _) = CreateFactory();
        var expectedEvaluations = CreateSampleEvaluations(3);
        var wrapper = new EvidenceHypothesisEvaluationResult { Evaluations = expectedEvaluations };

        // Act - Use reflection to call protected method
        var methodInfo = typeof(EvidenceHypothesisEvaluationOrchestrationFactory)
            .GetMethod("UnwrapResult",
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Instance);
        var result = methodInfo?.Invoke(factory, new object[] { wrapper }) as List<EvidenceHypothesisEvaluation>;

        // Assert
        Assert.NotNull(result);
        Assert.Same(expectedEvaluations, result);
        Assert.Equal(3, result.Count);
    }

    /// <summary>
    /// WHY: UnwrapResult should handle empty evaluation lists correctly.
    /// An empty result is valid when no evaluations are found.
    /// </summary>
    [Fact]
    public void UnwrapResult_WithEmptyWrapper_ReturnsEmptyList()
    {
        // Arrange
        var (factory, _, _, _, _) = CreateFactory();
        var wrapper = new EvidenceHypothesisEvaluationResult { Evaluations = new List<EvidenceHypothesisEvaluation>() };

        // Act
        var methodInfo = typeof(EvidenceHypothesisEvaluationOrchestrationFactory)
            .GetMethod("UnwrapResult",
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Instance);
        var result = methodInfo?.Invoke(factory, new object[] { wrapper }) as List<EvidenceHypothesisEvaluation>;

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    /// <summary>
    /// WHY: UnwrapResult should preserve all evaluation properties.
    /// The unwrapped data should be identical to the input.
    /// </summary>
    [Fact]
    public void UnwrapResult_PreservesEvaluationProperties()
    {
        // Arrange
        var (factory, _, _, _, _) = CreateFactory();
        var evaluation = new EvidenceHypothesisEvaluation
        {
            Hypothesis = new Hypothesis
            {
                ShortTitle = "Test Hypothesis",
                HypothesisText = "Detailed hypothesis text for testing"
            },
            Evidence = new Evidence
            {
                EvidenceId = Guid.NewGuid(),
                Claim = "Specific test claim",
                ReferenceSnippet = "Reference snippet text",
                Type = EvidenceType.Fact,
                Notes = "Detailed notes"
            },
            Score = EvaluationScore.Consistent,
            ScoreRationale = "Evidence strongly supports hypothesis",
            ConfidenceLevel = 0.85m,
            ConfidenceRationale = "High confidence based on multiple sources"
        };
        var wrapper = new EvidenceHypothesisEvaluationResult { Evaluations = new List<EvidenceHypothesisEvaluation> { evaluation } };

        // Act
        var methodInfo = typeof(EvidenceHypothesisEvaluationOrchestrationFactory)
            .GetMethod("UnwrapResult",
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Instance);
        var result = methodInfo?.Invoke(factory, new object[] { wrapper }) as List<EvidenceHypothesisEvaluation>;

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        var unwrappedEvaluation = result[0];
        Assert.Equal(evaluation.Hypothesis.ShortTitle, unwrappedEvaluation.Hypothesis.ShortTitle);
        Assert.Equal(evaluation.Hypothesis.HypothesisText, unwrappedEvaluation.Hypothesis.HypothesisText);
        Assert.Equal(evaluation.Evidence.EvidenceId, unwrappedEvaluation.Evidence.EvidenceId);
        Assert.Equal(evaluation.Evidence.Claim, unwrappedEvaluation.Evidence.Claim);
        Assert.Equal(evaluation.Score, unwrappedEvaluation.Score);
        Assert.Equal(evaluation.ScoreRationale, unwrappedEvaluation.ScoreRationale);
        Assert.Equal(evaluation.ConfidenceLevel, unwrappedEvaluation.ConfidenceLevel);
        Assert.Equal(evaluation.ConfidenceRationale, unwrappedEvaluation.ConfidenceRationale);
    }

    #endregion

    #region GetItemCount Tests

    /// <summary>
    /// WHY: GetItemCount should return the number of evaluation items.
    /// This is used for logging orchestration output volume.
    /// </summary>
    [Fact]
    public void GetItemCount_WithMultipleItems_ReturnsCorrectCount()
    {
        // Arrange
        var (factory, _, _, _, _) = CreateFactory();
        var evaluations = CreateSampleEvaluations(5);

        // Act
        var methodInfo = typeof(EvidenceHypothesisEvaluationOrchestrationFactory)
            .GetMethod("GetItemCount",
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Instance);
        var result = methodInfo?.Invoke(factory, new object[] { evaluations });

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
        var emptyEvaluations = new List<EvidenceHypothesisEvaluation>();

        // Act
        var methodInfo = typeof(EvidenceHypothesisEvaluationOrchestrationFactory)
            .GetMethod("GetItemCount",
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Instance);
        var result = methodInfo?.Invoke(factory, new object[] { emptyEvaluations });

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
        var singleEvaluation = CreateSampleEvaluations(1);

        // Act
        var methodInfo = typeof(EvidenceHypothesisEvaluationOrchestrationFactory)
            .GetMethod("GetItemCount",
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Instance);
        var result = methodInfo?.Invoke(factory, new object[] { singleEvaluation });

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
        var largeEvaluations = CreateSampleEvaluations(1000);

        // Act
        var methodInfo = typeof(EvidenceHypothesisEvaluationOrchestrationFactory)
            .GetMethod("GetItemCount",
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Instance);
        var result = methodInfo?.Invoke(factory, new object[] { largeEvaluations });

        // Assert
        Assert.Equal(1000, result);
    }

    #endregion

    #region CreateEmptyResult Tests

    /// <summary>
    /// WHY: CreateEmptyResult should return an empty list when no evaluations are found.
    /// This prevents null reference exceptions in the calling code.
    /// </summary>
    [Fact]
    public void CreateEmptyResult_Always_ReturnsEmptyList()
    {
        // Arrange
        var (factory, _, _, _, _) = CreateFactory();

        // Act
        var methodInfo = typeof(EvidenceHypothesisEvaluationOrchestrationFactory)
            .GetMethod("CreateEmptyResult",
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Instance);
        var result = methodInfo?.Invoke(factory, null) as List<EvidenceHypothesisEvaluation>;

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
        var methodInfo = typeof(EvidenceHypothesisEvaluationOrchestrationFactory)
            .GetMethod("CreateEmptyResult",
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Instance);

        // Act
        var result1 = methodInfo?.Invoke(factory, null) as List<EvidenceHypothesisEvaluation>;
        var result2 = methodInfo?.Invoke(factory, null) as List<EvidenceHypothesisEvaluation>;

        // Assert
        Assert.NotNull(result1);
        Assert.NotNull(result2);
        Assert.NotSame(result1, result2);
    }

    /// <summary>
    /// WHY: CreateEmptyResult should return correct type (not Evidence list).
    /// Ensures we're returning EvidenceHypothesisEvaluation list.
    /// </summary>
    [Fact]
    public void CreateEmptyResult_ReturnsCorrectType()
    {
        // Arrange
        var (factory, _, _, _, _) = CreateFactory();

        // Act
        var methodInfo = typeof(EvidenceHypothesisEvaluationOrchestrationFactory)
            .GetMethod("CreateEmptyResult",
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Instance);
        var result = methodInfo?.Invoke(factory, null);

        // Assert
        Assert.IsType<List<EvidenceHypothesisEvaluation>>(result);
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
        var methodInfo = typeof(EvidenceHypothesisEvaluationOrchestrationFactory)
            .GetMethod("CreateErrorResult",
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Instance);
        var result = methodInfo?.Invoke(factory, null) as List<EvidenceHypothesisEvaluation>;

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
        var methodInfo = typeof(EvidenceHypothesisEvaluationOrchestrationFactory)
            .GetMethod("CreateErrorResult",
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Instance);

        // Act
        var result1 = methodInfo?.Invoke(factory, null) as List<EvidenceHypothesisEvaluation>;
        var result2 = methodInfo?.Invoke(factory, null) as List<EvidenceHypothesisEvaluation>;

        // Assert
        Assert.NotNull(result1);
        Assert.NotNull(result2);
        Assert.NotSame(result1, result2);
    }

    /// <summary>
    /// WHY: CreateErrorResult should return correct type.
    /// Ensures we're returning EvidenceHypothesisEvaluation list.
    /// </summary>
    [Fact]
    public void CreateErrorResult_ReturnsCorrectType()
    {
        // Arrange
        var (factory, _, _, _, _) = CreateFactory();

        // Act
        var methodInfo = typeof(EvidenceHypothesisEvaluationOrchestrationFactory)
            .GetMethod("CreateErrorResult",
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Instance);
        var result = methodInfo?.Invoke(factory, null);

        // Assert
        Assert.IsType<List<EvidenceHypothesisEvaluation>>(result);
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
        var methodInfo = typeof(EvidenceHypothesisEvaluationOrchestrationFactory)
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
        var methodInfo = typeof(EvidenceHypothesisEvaluationOrchestrationFactory)
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
        var methodInfo = typeof(EvidenceHypothesisEvaluationOrchestrationFactory)
            .GetMethod("GetAgentSelectionReason",
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Instance);
        var result = methodInfo?.Invoke(factory, new object?[] { null }) as string;

        // Assert
        Assert.NotNull(result);
        Assert.Contains("simultaneous", result, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// WHY: Ensures the message does NOT mention "Sequential selection".
    /// This guards against the bug where ConcurrentOrchestration had a sequential message.
    /// </summary>
    [Fact]
    public void GetAgentSelectionReason_DoesNotMentionSequential()
    {
        // Arrange
        var (factory, _, _, _, _) = CreateFactory();

        // Act
        var methodInfo = typeof(EvidenceHypothesisEvaluationOrchestrationFactory)
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
        Assert.IsAssignableFrom<IOrchestrationFactory<List<EvidenceHypothesisEvaluation>>>(factory);
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
        Assert.IsAssignableFrom<BaseOrchestrationFactory<List<EvidenceHypothesisEvaluation>, EvidenceHypothesisEvaluationResult>>(factory);
    }

    #endregion

    #region EvaluationScore Tests

    /// <summary>
    /// WHY: UnwrapResult should correctly preserve Consistent evaluation score.
    /// EvaluationScore is critical for ACH matrix analysis.
    /// </summary>
    [Fact]
    public void UnwrapResult_PreservesConsistentScore()
    {
        // Arrange
        var (factory, _, _, _, _) = CreateFactory();
        var evaluation = new EvidenceHypothesisEvaluation
        {
            Hypothesis = new Hypothesis { ShortTitle = "H1" },
            Evidence = new Evidence { Claim = "E1" },
            Score = EvaluationScore.Consistent,
            ScoreRationale = "Test rationale"
        };
        var wrapper = new EvidenceHypothesisEvaluationResult { Evaluations = new List<EvidenceHypothesisEvaluation> { evaluation } };

        // Act
        var methodInfo = typeof(EvidenceHypothesisEvaluationOrchestrationFactory)
            .GetMethod("UnwrapResult",
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Instance);
        var result = methodInfo?.Invoke(factory, new object[] { wrapper }) as List<EvidenceHypothesisEvaluation>;

        // Assert
        Assert.NotNull(result);
        Assert.Equal(EvaluationScore.Consistent, result[0].Score);
    }

    /// <summary>
    /// WHY: UnwrapResult should correctly preserve Inconsistent evaluation score.
    /// Identifying inconsistencies is key to disproving hypotheses in ACH.
    /// </summary>
    [Fact]
    public void UnwrapResult_PreservesInconsistentScore()
    {
        // Arrange
        var (factory, _, _, _, _) = CreateFactory();
        var evaluation = new EvidenceHypothesisEvaluation
        {
            Hypothesis = new Hypothesis { ShortTitle = "H1" },
            Evidence = new Evidence { Claim = "E1" },
            Score = EvaluationScore.Inconsistent,
            ScoreRationale = "Test rationale"
        };
        var wrapper = new EvidenceHypothesisEvaluationResult { Evaluations = new List<EvidenceHypothesisEvaluation> { evaluation } };

        // Act
        var methodInfo = typeof(EvidenceHypothesisEvaluationOrchestrationFactory)
            .GetMethod("UnwrapResult",
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Instance);
        var result = methodInfo?.Invoke(factory, new object[] { wrapper }) as List<EvidenceHypothesisEvaluation>;

        // Assert
        Assert.NotNull(result);
        Assert.Equal(EvaluationScore.Inconsistent, result[0].Score);
    }

    /// <summary>
    /// WHY: UnwrapResult should correctly preserve Neutral evaluation score.
    /// Neutral evidence neither supports nor refutes a hypothesis.
    /// </summary>
    [Fact]
    public void UnwrapResult_PreservesNeutralScore()
    {
        // Arrange
        var (factory, _, _, _, _) = CreateFactory();
        var evaluation = new EvidenceHypothesisEvaluation
        {
            Hypothesis = new Hypothesis { ShortTitle = "H1" },
            Evidence = new Evidence { Claim = "E1" },
            Score = EvaluationScore.Neutral,
            ScoreRationale = "Test rationale"
        };
        var wrapper = new EvidenceHypothesisEvaluationResult { Evaluations = new List<EvidenceHypothesisEvaluation> { evaluation } };

        // Act
        var methodInfo = typeof(EvidenceHypothesisEvaluationOrchestrationFactory)
            .GetMethod("UnwrapResult",
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Instance);
        var result = methodInfo?.Invoke(factory, new object[] { wrapper }) as List<EvidenceHypothesisEvaluation>;

        // Assert
        Assert.NotNull(result);
        Assert.Equal(EvaluationScore.Neutral, result[0].Score);
    }

    #endregion

    #region ConfidenceLevel Tests

    /// <summary>
    /// WHY: UnwrapResult should preserve confidence level values.
    /// Confidence levels help assess evaluation reliability.
    /// </summary>
    [Theory]
    [InlineData(0.0)]
    [InlineData(0.5)]
    [InlineData(1.0)]
    [InlineData(0.75)]
    public void UnwrapResult_PreservesConfidenceLevel(decimal confidenceLevel)
    {
        // Arrange
        var (factory, _, _, _, _) = CreateFactory();
        var evaluation = new EvidenceHypothesisEvaluation
        {
            Hypothesis = new Hypothesis { ShortTitle = "H1" },
            Evidence = new Evidence { Claim = "E1" },
            Score = EvaluationScore.Consistent,
            ConfidenceLevel = confidenceLevel
        };
        var wrapper = new EvidenceHypothesisEvaluationResult { Evaluations = new List<EvidenceHypothesisEvaluation> { evaluation } };

        // Act
        var methodInfo = typeof(EvidenceHypothesisEvaluationOrchestrationFactory)
            .GetMethod("UnwrapResult",
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Instance);
        var result = methodInfo?.Invoke(factory, new object[] { wrapper }) as List<EvidenceHypothesisEvaluation>;

        // Assert
        Assert.NotNull(result);
        Assert.Equal(confidenceLevel, result[0].ConfidenceLevel);
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

        var methodInfo = typeof(EvidenceHypothesisEvaluationOrchestrationFactory)
            .GetMethod("CreateEmptyResult",
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Instance);

        // Act
        var result1 = methodInfo?.Invoke(factory1, null) as List<EvidenceHypothesisEvaluation>;
        var result2 = methodInfo?.Invoke(factory2, null) as List<EvidenceHypothesisEvaluation>;

        // Assert
        Assert.NotNull(result1);
        Assert.NotNull(result2);
        Assert.NotSame(result1, result2);
    }

    #endregion
}
