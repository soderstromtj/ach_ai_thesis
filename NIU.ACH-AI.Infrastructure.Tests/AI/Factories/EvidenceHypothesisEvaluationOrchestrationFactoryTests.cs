using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using NIU.ACH_AI.Application.Configuration;
using NIU.ACH_AI.Application.DTOs;
using NIU.ACH_AI.Application.Interfaces;
using NIU.ACH_AI.Domain.Entities;
using NIU.ACH_AI.Infrastructure.AI.Factories;
using System.Reflection;

namespace NIU.ACH_AI.Infrastructure.Tests.AI.Factories;

/// <summary>
/// Unit tests for EvidenceHypothesisEvaluationOrchestrationFactory.
///
/// Testing Strategy:
/// -----------------
/// EvidenceHypothesisEvaluationOrchestrationFactory is a CONCRETE class that extends BaseOrchestrationFactory.
/// The tests focus on the specific implementations of the abstract methods provided by this factory.
///
/// What We Test:
/// 1. Constructor - Verifies proper initialization and base class integration
/// 2. CreateLogger - Returns correctly typed logger
/// 3. GetResultTypeName - Returns "EvidenceHypothesisEvaluationResult"
/// 4. UnwrapResult - Returns the input wrapper directly (Identity transformation)
/// 5. GetItemCount - Returns 1 (fixed count)
/// 6. CreateEmptyResult - Returns new EvidenceHypothesisEvaluation
/// 7. CreateErrorResult - Returns new EvidenceHypothesisEvaluation
/// 8. GetAgentSelectionReason - Returns concurrent execution message
///
/// Note: CreateOrchestration is excluded as it creates Semantic Kernel orchestration objects
/// which are complex to mock and better suited for integration tests.
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
        Mock<ILogger<EvidenceHypothesisEvaluationOrchestrationFactory>> LoggerMock,
        Mock<IAgentResponsePersistence> AgentResponsePersistenceMock) CreateFactory(
        OrchestrationSettings? settings = null)
    {
        var agentServiceMock = new Mock<IAgentService>();
        var kernelBuilderServiceMock = new Mock<IKernelBuilderService>();
        var loggerFactoryMock = new Mock<ILoggerFactory>();
        var loggerMock = new Mock<ILogger<EvidenceHypothesisEvaluationOrchestrationFactory>>();
        var agentResponsePersistenceMock = new Mock<IAgentResponsePersistence>();

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

        var orchestrationPromptFormatterMock = new Mock<IOrchestrationPromptFormatter>();

        var factory = new EvidenceHypothesisEvaluationOrchestrationFactory(
                agentServiceMock.Object,
                kernelBuilderServiceMock.Object,
                Options.Create(orchestrationSettings),
                orchestrationPromptFormatterMock.Object,
                loggerFactoryMock.Object,
                agentResponsePersistenceMock.Object);

        return (factory, agentServiceMock, kernelBuilderServiceMock, loggerFactoryMock, loggerMock, agentResponsePersistenceMock);
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
        var (factory, _, _, _, _, _) = CreateFactory();

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
            new Mock<IOrchestrationPromptFormatter>().Object,
            loggerFactoryMock.Object);

        // Assert
        Assert.NotNull(capturedCategory);
        Assert.Contains("EvidenceHypothesisEvaluationOrchestrationFactory", capturedCategory);
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
        var (factory, _, _, _, _, _) = CreateFactory();

        // Act - Use reflection to call protected method
        var methodInfo = typeof(EvidenceHypothesisEvaluationOrchestrationFactory)
            .GetMethod("GetResultTypeName",
                BindingFlags.NonPublic |
                BindingFlags.Instance);
        var result = methodInfo?.Invoke(factory, null) as string;

        // Assert
        Assert.Equal("EvidenceHypothesisEvaluationResult", result);
    }

    #endregion

    #region UnwrapResult Tests

    /// <summary>
    /// WHY: UnwrapResult should simply return the input wrapper in this case.
    /// The TWrapper and TResult are the same logic for this factory.
    /// </summary>
    [Fact]
    public void UnwrapResult_ReturnsInputWrapper()
    {
        // Arrange
        var (factory, _, _, _, _, _) = CreateFactory();
        var inputEvaluation = new EvidenceHypothesisEvaluation();

        // Act
        var methodInfo = typeof(EvidenceHypothesisEvaluationOrchestrationFactory)
            .GetMethod("UnwrapResult",
                BindingFlags.NonPublic |
                BindingFlags.Instance);
        var result = methodInfo?.Invoke(factory, new object[] { inputEvaluation }) as EvidenceHypothesisEvaluation;

        // Assert
        Assert.NotNull(result);
        Assert.Same(inputEvaluation, result);
    }

    /// <summary>
    /// WHY: UnwrapResult should handle null input if that logic were present,
    /// but based on signature it expects a class. We verify it passes through whatever it gets.
    /// </summary>
    [Fact]
    public void UnwrapResult_WithNewInstance_ReturnsDifferentInstance()
    {
        // Arrange
        var (factory, _, _, _, _, _) = CreateFactory();
        var eval1 = new EvidenceHypothesisEvaluation();
        var eval2 = new EvidenceHypothesisEvaluation();

        var methodInfo = typeof(EvidenceHypothesisEvaluationOrchestrationFactory)
            .GetMethod("UnwrapResult",
                BindingFlags.NonPublic |
                BindingFlags.Instance);

        // Act
        var result1 = methodInfo?.Invoke(factory, new object[] { eval1 }) as EvidenceHypothesisEvaluation;
        var result2 = methodInfo?.Invoke(factory, new object[] { eval2 }) as EvidenceHypothesisEvaluation;

        // Assert
        Assert.NotSame(result1, result2);
        Assert.Same(eval1, result1);
        Assert.Same(eval2, result2);
    }

    #endregion

    #region GetItemCount Tests

    /// <summary>
    /// WHY: GetItemCount should always return 1 for this factory type.
    /// The evaluation result is a single object, not a list.
    /// </summary>
    [Fact]
    public void GetItemCount_Always_ReturnsOne()
    {
        // Arrange
        var (factory, _, _, _, _, _) = CreateFactory();
        var evaluation = new EvidenceHypothesisEvaluation();

        // Act
        var methodInfo = typeof(EvidenceHypothesisEvaluationOrchestrationFactory)
            .GetMethod("GetItemCount",
                BindingFlags.NonPublic |
                BindingFlags.Instance);
        var result = methodInfo?.Invoke(factory, new object[] { evaluation });

        // Assert
        Assert.Equal(1, result);
    }

    #endregion

    #region CreateEmptyResult Tests

    /// <summary>
    /// WHY: CreateEmptyResult should return a new empty instance.
    /// Prevents null issues in calling code when transformation fails.
    /// </summary>
    [Fact]
    public void CreateEmptyResult_Always_ReturnsNewInstance()
    {
        // Arrange
        var (factory, _, _, _, _, _) = CreateFactory();

        // Act
        var methodInfo = typeof(EvidenceHypothesisEvaluationOrchestrationFactory)
            .GetMethod("CreateEmptyResult",
                BindingFlags.NonPublic |
                BindingFlags.Instance);
        var result = methodInfo?.Invoke(factory, null) as EvidenceHypothesisEvaluation;

        // Assert
        Assert.NotNull(result);
    }

    /// <summary>
    /// WHY: Verify that CreateEmptyResult returns unique instances.
    /// Important for data isolation.
    /// </summary>
    [Fact]
    public void CreateEmptyResult_CalledTwice_ReturnsDifferentInstances()
    {
        // Arrange
        var (factory, _, _, _, _, _) = CreateFactory();
        var methodInfo = typeof(EvidenceHypothesisEvaluationOrchestrationFactory)
            .GetMethod("CreateEmptyResult",
                BindingFlags.NonPublic |
                BindingFlags.Instance);

        // Act
        var result1 = methodInfo?.Invoke(factory, null) as EvidenceHypothesisEvaluation;
        var result2 = methodInfo?.Invoke(factory, null) as EvidenceHypothesisEvaluation;

        // Assert
        Assert.NotNull(result1);
        Assert.NotNull(result2);
        Assert.NotSame(result1, result2);
    }

    #endregion

    #region CreateErrorResult Tests

    /// <summary>
    /// WHY: CreateErrorResult should return a new empty instance.
    /// Allows graceful failure handling.
    /// </summary>
    [Fact]
    public void CreateErrorResult_Always_ReturnsNewInstance()
    {
        // Arrange
        var (factory, _, _, _, _, _) = CreateFactory();

        // Act
        var methodInfo = typeof(EvidenceHypothesisEvaluationOrchestrationFactory)
            .GetMethod("CreateErrorResult",
                BindingFlags.NonPublic |
                BindingFlags.Instance);
        var result = methodInfo?.Invoke(factory, null) as EvidenceHypothesisEvaluation;

        // Assert
        Assert.NotNull(result);
    }

    #endregion

    #region GetAgentSelectionReason Tests

    /// <summary>
    /// WHY: GetAgentSelectionReason should indicate concurrent execution.
    /// Helpful for debugging logs.
    /// </summary>
    [Fact]
    public void GetAgentSelectionReason_Always_ReturnsConcurrentMessage()
    {
        // Arrange
        var (factory, _, _, _, _, _) = CreateFactory();

        // Act
        var methodInfo = typeof(EvidenceHypothesisEvaluationOrchestrationFactory)
            .GetMethod("GetAgentSelectionReason",
                BindingFlags.NonPublic |
                BindingFlags.Instance);
        var result = methodInfo?.Invoke(factory, new object?[] { "PreviousAgent" }) as string;

        // Assert
        Assert.NotNull(result);
        Assert.Contains("Concurrent execution", result, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("simultaneously", result, StringComparison.OrdinalIgnoreCase);
    }

    #endregion

    #region Interface Implementation Tests

    /// <summary>
    /// WHY: The factory should implement IOrchestrationFactory interface.
    /// Ensures potential use in DI or generic collections.
    /// </summary>
    [Fact]
    public void Factory_ImplementsIOrchestrationFactory()
    {
        // Arrange
        var (factory, _, _, _, _, _) = CreateFactory();

        // Assert
        Assert.IsAssignableFrom<IOrchestrationFactory<EvidenceHypothesisEvaluation>>(factory);
    }

    /// <summary>
    /// WHY: The factory should inherit from BaseOrchestrationFactory.
    /// Ensures correct inheritance hierarchy.
    /// </summary>
    [Fact]
    public void Factory_InheritsFromBaseOrchestrationFactory()
    {
        // Arrange
        var (factory, _, _, _, _, _) = CreateFactory();

        // Assert
        Assert.IsAssignableFrom<BaseOrchestrationFactory<EvidenceHypothesisEvaluation, EvidenceHypothesisEvaluation>>(factory);
    }

    #endregion
}
