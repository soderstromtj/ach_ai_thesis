using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel.Agents;
using Moq;
using NIU.ACH_AI.Application.Configuration;
using NIU.ACH_AI.Application.DTOs;
using NIU.ACH_AI.Application.Interfaces;
using NIU.ACH_AI.Domain.Entities;
using NIU.ACH_AI.Domain.ValueObjects;
using NIU.ACH_AI.Infrastructure.AI.Factories;

namespace NIU.ACH_AI.Infrastructure.Tests.AI.Factories;

/// <summary>
/// Unit tests for EvidenceExtractionOrchestrationFactory.
///
/// Testing Strategy:
/// -----------------
/// EvidenceExtractionOrchestrationFactory is a CONCRETE class that extends BaseOrchestrationFactory.
/// Unlike the base class tests, we can instantiate this class directly and test its specific
/// implementations of the abstract methods.
///
/// What We Test:
/// 1. Constructor - Verifies proper initialization and base class integration
/// 2. CreateLogger - Returns correctly typed logger
/// 3. GetResultTypeName - Returns "EvidenceResult"
/// 4. UnwrapResult - Extracts Evidence list from EvidenceResult wrapper
/// 5. GetItemCount - Returns correct count of evidence items
/// 6. CreateEmptyResult - Returns empty List<Evidence>
/// 7. CreateErrorResult - Returns empty List<Evidence>
/// 8. GetAgentSelectionReason - Returns concurrent execution message
///
/// Note: CreateOrchestration is not directly unit-testable as it creates Semantic Kernel
/// orchestration objects. That method is better suited for integration testing.
/// </summary>
public class EvidenceExtractionOrchestrationFactoryTests
{
    #region Test Infrastructure

    /// <summary>
    /// Creates a factory instance with all dependencies mocked.
    /// </summary>
    private static (EvidenceExtractionOrchestrationFactory Factory,
        Mock<IAgentService> AgentServiceMock,
        Mock<IKernelBuilderService> KernelBuilderServiceMock,
        Mock<ILoggerFactory> LoggerFactoryMock,
        Mock<ILogger<EvidenceExtractionOrchestrationFactory>> LoggerMock) CreateFactory(
        OrchestrationSettings? settings = null)
    {
        var agentServiceMock = new Mock<IAgentService>();
        var kernelBuilderServiceMock = new Mock<IKernelBuilderService>();
        var loggerFactoryMock = new Mock<ILoggerFactory>();
        var loggerMock = new Mock<ILogger<EvidenceExtractionOrchestrationFactory>>();

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

        var factory = new EvidenceExtractionOrchestrationFactory(
            agentServiceMock.Object,
            kernelBuilderServiceMock.Object,
            optionsMock.Object,
            loggerFactoryMock.Object);

        return (factory, agentServiceMock, kernelBuilderServiceMock, loggerFactoryMock, loggerMock);
    }

    /// <summary>
    /// Creates sample evidence for testing.
    /// </summary>
    private static List<Evidence> CreateSampleEvidence(int count)
    {
        var evidence = new List<Evidence>();
        for (int i = 0; i < count; i++)
        {
            evidence.Add(new Evidence
            {
                EvidenceId = Guid.NewGuid(),
                Claim = $"Test claim {i + 1}",
                ReferenceSnippet = $"Reference for claim {i + 1}",
                Type = i % 2 == 0 ? EvidenceType.Fact : EvidenceType.Assumption,
                Notes = $"Notes for evidence {i + 1}"
            });
        }
        return evidence;
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
        var factory = new EvidenceExtractionOrchestrationFactory(
            Mock.Of<IAgentService>(),
            Mock.Of<IKernelBuilderService>(),
            optionsMock.Object,
            loggerFactoryMock.Object);

        // Assert
        Assert.NotNull(capturedCategory);
        Assert.Contains("EvidenceExtractionOrchestrationFactory", capturedCategory);
    }

    #endregion

    #region GetResultTypeName Tests

    /// <summary>
    /// WHY: GetResultTypeName should return the wrapper type name for logging.
    /// This helps identify what type of structured output is expected.
    /// </summary>
    [Fact]
    public void GetResultTypeName_Always_ReturnsEvidenceResult()
    {
        // Arrange
        var (factory, _, _, _, _) = CreateFactory();

        // Act - Use reflection to call protected method
        var methodInfo = typeof(EvidenceExtractionOrchestrationFactory)
            .GetMethod("GetResultTypeName",
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Instance);
        var result = methodInfo?.Invoke(factory, null) as string;

        // Assert
        Assert.Equal("EvidenceResult", result);
    }

    #endregion

    #region UnwrapResult Tests

    /// <summary>
    /// WHY: UnwrapResult extracts the Evidence list from the EvidenceResult wrapper.
    /// This is critical for returning the actual data to callers.
    /// </summary>
    [Fact]
    public void UnwrapResult_WithPopulatedWrapper_ReturnsEvidenceList()
    {
        // Arrange
        var (factory, _, _, _, _) = CreateFactory();
        var expectedEvidence = CreateSampleEvidence(3);
        var wrapper = new EvidenceResult { Evidence = expectedEvidence };

        // Act - Use reflection to call protected method
        var methodInfo = typeof(EvidenceExtractionOrchestrationFactory)
            .GetMethod("UnwrapResult",
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Instance);
        var result = methodInfo?.Invoke(factory, new object[] { wrapper }) as List<Evidence>;

        // Assert
        Assert.NotNull(result);
        Assert.Same(expectedEvidence, result);
        Assert.Equal(3, result.Count);
    }

    /// <summary>
    /// WHY: UnwrapResult should handle empty evidence lists correctly.
    /// An empty result is valid when no evidence is found.
    /// </summary>
    [Fact]
    public void UnwrapResult_WithEmptyWrapper_ReturnsEmptyList()
    {
        // Arrange
        var (factory, _, _, _, _) = CreateFactory();
        var wrapper = new EvidenceResult { Evidence = new List<Evidence>() };

        // Act
        var methodInfo = typeof(EvidenceExtractionOrchestrationFactory)
            .GetMethod("UnwrapResult",
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Instance);
        var result = methodInfo?.Invoke(factory, new object[] { wrapper }) as List<Evidence>;

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    /// <summary>
    /// WHY: UnwrapResult should preserve all evidence properties.
    /// The unwrapped data should be identical to the input.
    /// </summary>
    [Fact]
    public void UnwrapResult_PreservesEvidenceProperties()
    {
        // Arrange
        var (factory, _, _, _, _) = CreateFactory();
        var evidence = new Evidence
        {
            EvidenceId = Guid.NewGuid(),
            Claim = "Test claim with specific content",
            ReferenceSnippet = "Specific reference snippet",
            Type = EvidenceType.Fact,
            Notes = "Detailed notes"
        };
        var wrapper = new EvidenceResult { Evidence = new List<Evidence> { evidence } };

        // Act
        var methodInfo = typeof(EvidenceExtractionOrchestrationFactory)
            .GetMethod("UnwrapResult",
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Instance);
        var result = methodInfo?.Invoke(factory, new object[] { wrapper }) as List<Evidence>;

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        var unwrappedEvidence = result[0];
        Assert.Equal(evidence.EvidenceId, unwrappedEvidence.EvidenceId);
        Assert.Equal(evidence.Claim, unwrappedEvidence.Claim);
        Assert.Equal(evidence.ReferenceSnippet, unwrappedEvidence.ReferenceSnippet);
        Assert.Equal(evidence.Type, unwrappedEvidence.Type);
        Assert.Equal(evidence.Notes, unwrappedEvidence.Notes);
    }

    #endregion

    #region GetItemCount Tests

    /// <summary>
    /// WHY: GetItemCount should return the number of evidence items.
    /// This is used for logging orchestration output volume.
    /// </summary>
    [Fact]
    public void GetItemCount_WithMultipleItems_ReturnsCorrectCount()
    {
        // Arrange
        var (factory, _, _, _, _) = CreateFactory();
        var evidence = CreateSampleEvidence(5);

        // Act
        var methodInfo = typeof(EvidenceExtractionOrchestrationFactory)
            .GetMethod("GetItemCount",
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Instance);
        var result = methodInfo?.Invoke(factory, new object[] { evidence });

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
        var emptyEvidence = new List<Evidence>();

        // Act
        var methodInfo = typeof(EvidenceExtractionOrchestrationFactory)
            .GetMethod("GetItemCount",
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Instance);
        var result = methodInfo?.Invoke(factory, new object[] { emptyEvidence });

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
        var singleEvidence = CreateSampleEvidence(1);

        // Act
        var methodInfo = typeof(EvidenceExtractionOrchestrationFactory)
            .GetMethod("GetItemCount",
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Instance);
        var result = methodInfo?.Invoke(factory, new object[] { singleEvidence });

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
        var largeEvidence = CreateSampleEvidence(1000);

        // Act
        var methodInfo = typeof(EvidenceExtractionOrchestrationFactory)
            .GetMethod("GetItemCount",
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Instance);
        var result = methodInfo?.Invoke(factory, new object[] { largeEvidence });

        // Assert
        Assert.Equal(1000, result);
    }

    #endregion

    #region CreateEmptyResult Tests

    /// <summary>
    /// WHY: CreateEmptyResult should return an empty list when no evidence is found.
    /// This prevents null reference exceptions in the calling code.
    /// </summary>
    [Fact]
    public void CreateEmptyResult_Always_ReturnsEmptyList()
    {
        // Arrange
        var (factory, _, _, _, _) = CreateFactory();

        // Act
        var methodInfo = typeof(EvidenceExtractionOrchestrationFactory)
            .GetMethod("CreateEmptyResult",
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Instance);
        var result = methodInfo?.Invoke(factory, null) as List<Evidence>;

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
        var methodInfo = typeof(EvidenceExtractionOrchestrationFactory)
            .GetMethod("CreateEmptyResult",
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Instance);

        // Act
        var result1 = methodInfo?.Invoke(factory, null) as List<Evidence>;
        var result2 = methodInfo?.Invoke(factory, null) as List<Evidence>;

        // Assert
        Assert.NotNull(result1);
        Assert.NotNull(result2);
        Assert.NotSame(result1, result2);
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
        var methodInfo = typeof(EvidenceExtractionOrchestrationFactory)
            .GetMethod("CreateErrorResult",
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Instance);
        var result = methodInfo?.Invoke(factory, null) as List<Evidence>;

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
        var methodInfo = typeof(EvidenceExtractionOrchestrationFactory)
            .GetMethod("CreateErrorResult",
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Instance);

        // Act
        var result1 = methodInfo?.Invoke(factory, null) as List<Evidence>;
        var result2 = methodInfo?.Invoke(factory, null) as List<Evidence>;

        // Assert
        Assert.NotNull(result1);
        Assert.NotNull(result2);
        Assert.NotSame(result1, result2);
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
        var methodInfo = typeof(EvidenceExtractionOrchestrationFactory)
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
        var methodInfo = typeof(EvidenceExtractionOrchestrationFactory)
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
        var methodInfo = typeof(EvidenceExtractionOrchestrationFactory)
            .GetMethod("GetAgentSelectionReason",
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Instance);
        var result = methodInfo?.Invoke(factory, new object?[] { null }) as string;

        // Assert
        Assert.NotNull(result);
        Assert.Contains("simultaneous", result, StringComparison.OrdinalIgnoreCase);
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
        Assert.IsAssignableFrom<IOrchestrationFactory<List<Evidence>>>(factory);
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
        Assert.IsAssignableFrom<BaseOrchestrationFactory<List<Evidence>, EvidenceResult>>(factory);
    }

    #endregion

    #region Evidence Type Tests

    /// <summary>
    /// WHY: UnwrapResult should correctly preserve Fact evidence type.
    /// EvidenceType is critical for ACH analysis.
    /// </summary>
    [Fact]
    public void UnwrapResult_PreservesFactEvidenceType()
    {
        // Arrange
        var (factory, _, _, _, _) = CreateFactory();
        var evidence = new Evidence { Claim = "Test", Type = EvidenceType.Fact };
        var wrapper = new EvidenceResult { Evidence = new List<Evidence> { evidence } };

        // Act
        var methodInfo = typeof(EvidenceExtractionOrchestrationFactory)
            .GetMethod("UnwrapResult",
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Instance);
        var result = methodInfo?.Invoke(factory, new object[] { wrapper }) as List<Evidence>;

        // Assert
        Assert.NotNull(result);
        Assert.Equal(EvidenceType.Fact, result[0].Type);
    }

    /// <summary>
    /// WHY: UnwrapResult should correctly preserve Assumption evidence type.
    /// Distinguishing facts from assumptions is core to ACH.
    /// </summary>
    [Fact]
    public void UnwrapResult_PreservesAssumptionEvidenceType()
    {
        // Arrange
        var (factory, _, _, _, _) = CreateFactory();
        var evidence = new Evidence { Claim = "Test", Type = EvidenceType.Assumption };
        var wrapper = new EvidenceResult { Evidence = new List<Evidence> { evidence } };

        // Act
        var methodInfo = typeof(EvidenceExtractionOrchestrationFactory)
            .GetMethod("UnwrapResult",
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Instance);
        var result = methodInfo?.Invoke(factory, new object[] { wrapper }) as List<Evidence>;

        // Assert
        Assert.NotNull(result);
        Assert.Equal(EvidenceType.Assumption, result[0].Type);
    }

    #endregion
}
