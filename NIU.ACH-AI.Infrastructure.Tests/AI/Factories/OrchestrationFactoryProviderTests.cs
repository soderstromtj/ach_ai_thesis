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
/// Unit tests for OrchestrationFactoryProvider.
///
/// Testing Strategy:
/// -----------------
/// OrchestrationFactoryProvider maps step names to specific IOrchestrationFactory implementations.
/// It uses IOrchestrationExecutor to resolve dependencies for these factories.
///
/// What We Test:
/// 1. Constructor - Dependency validation
/// 2. CreateFactory - Correct mapping for "Hypothesis Brainstorming" -> HypothesisBrainstormingOrchestrationFactory
/// 3. CreateFactory - Correct mapping for "Hypothesis Refinement" -> HypothesisRefinementOrchestrationFactory
/// 4. CreateFactory - Correct mapping for "Evidence Extraction" -> EvidenceExtractionOrchestrationFactory
/// 5. CreateFactory - Correct mapping for "Evidence Hypothesis Evaluation" -> EvidenceHypothesisEvaluationOrchestrationFactory
/// 6. CreateFactory - Case insensitivity and alias handling
/// 7. CreateFactory - Type mismatch validation (requesting wrong TResult)
/// 8. CreateFactory - Unknown step name handling (InvalidOperationException)
/// </summary>
public class OrchestrationFactoryProviderTests
{
    private readonly Mock<IOrchestrationExecutor> _orchestrationExecutorMock;
    private readonly Mock<IAgentResponsePersistence> _agentResponsePersistenceMock;
    private readonly Mock<IOrchestrationPromptFormatter> _promptFormatterMock;
    private readonly OrchestrationFactoryProvider _provider;

    // Infrastructure Mocks required for factory instantiation
    private readonly Mock<IAgentService> _agentServiceMock;
    private readonly Mock<IKernelBuilderService> _kernelBuilderServiceMock;
    private readonly Mock<ILoggerFactory> _loggerFactoryMock;
    private readonly Mock<IOptions<OrchestrationSettings>> _optionsMock;

    public OrchestrationFactoryProviderTests()
    {
        _orchestrationExecutorMock = new Mock<IOrchestrationExecutor>();
        _agentResponsePersistenceMock = new Mock<IAgentResponsePersistence>();
        _promptFormatterMock = new Mock<IOrchestrationPromptFormatter>();

        // Setup infrastructure mocks
        _agentServiceMock = new Mock<IAgentService>();
        _kernelBuilderServiceMock = new Mock<IKernelBuilderService>();
        _loggerFactoryMock = new Mock<ILoggerFactory>();
        _optionsMock = new Mock<IOptions<OrchestrationSettings>>();
        _optionsMock.Setup(o => o.Value).Returns(new OrchestrationSettings());

        // Setup Executor to return these mocks
        _orchestrationExecutorMock.Setup(e => e.CreateAgentService(It.IsAny<ACHStepConfiguration>()))
            .Returns(_agentServiceMock.Object);
        _orchestrationExecutorMock.Setup(e => e.GetKernelBuilderService())
            .Returns(_kernelBuilderServiceMock.Object);
        _orchestrationExecutorMock.Setup(e => e.GetLoggerFactory())
            .Returns(_loggerFactoryMock.Object);
        _orchestrationExecutorMock.Setup(e => e.CreateOrchestrationOptions(It.IsAny<ACHStepConfiguration>()))
            .Returns(_optionsMock.Object);
        
        // Setup internal logger creation for factories
        _loggerFactoryMock.Setup(x => x.CreateLogger(It.IsAny<string>()))
            .Returns(new Mock<ILogger>().Object);

        _provider = new OrchestrationFactoryProvider(
            _orchestrationExecutorMock.Object,
            _promptFormatterMock.Object,
            _agentResponsePersistenceMock.Object);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithValidDependencies_CreatesInstance()
    {
        // Act
        var provider = new OrchestrationFactoryProvider(
            _orchestrationExecutorMock.Object,
            _promptFormatterMock.Object,
            _agentResponsePersistenceMock.Object);

        // Assert
        Assert.NotNull(provider);
    }

    [Fact]
    public void Constructor_NullResponsePersistence_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new OrchestrationFactoryProvider(
                _orchestrationExecutorMock.Object,
                _promptFormatterMock.Object,
                null!));
    }

    [Fact]
    public void Constructor_NullPromptFormatter_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new OrchestrationFactoryProvider(
                _orchestrationExecutorMock.Object,
                null!,
                _agentResponsePersistenceMock.Object));
    }

    #endregion

    #region CreateFactory - Valid Mapping Tests

    [Theory]
    [InlineData("hypothesis brainstorming")]
    [InlineData("HypothesisBrainstorming")]
    public void CreateFactory_HypothesisBrainstorming_ReturnsCorrectFactory(string stepName)
    {
        // Arrange
        var config = new ACHStepConfiguration { Name = stepName };

        // Act
        var factory = _provider.CreateFactory<List<Hypothesis>>(config);

        // Assert
        Assert.NotNull(factory);
        Assert.IsType<HypothesisBrainstormingOrchestrationFactory>(factory);
    }

    [Theory]
    [InlineData("hypothesis refinement")]
    [InlineData("HypothesisRefinement")]
    [InlineData("hypothesis evaluation")]
    public void CreateFactory_HypothesisRefinement_ReturnsCorrectFactory(string stepName)
    {
        // Arrange
        var config = new ACHStepConfiguration { Name = stepName };

        // Act
        var factory = _provider.CreateFactory<List<Hypothesis>>(config);

        // Assert
        Assert.NotNull(factory);
        Assert.IsType<HypothesisRefinementOrchestrationFactory>(factory);
    }

    [Theory]
    [InlineData("evidence extraction")]
    [InlineData("EvidenceExtraction")]
    public void CreateFactory_EvidenceExtraction_ReturnsCorrectFactory(string stepName)
    {
        // Arrange
        var config = new ACHStepConfiguration { Name = stepName };

        // Act
        var factory = _provider.CreateFactory<List<Evidence>>(config);

        // Assert
        Assert.NotNull(factory);
        Assert.IsType<EvidenceExtractionOrchestrationFactory>(factory);
    }

    [Theory]
    [InlineData("evidence hypothesis evaluation")]
    [InlineData("EvidenceHypothesisEvaluation")]
    [InlineData("evidence evaluation")]
    public void CreateFactory_EvidenceHypothesisEvaluation_ReturnsCorrectFactory(string stepName)
    {
        // Arrange
        var config = new ACHStepConfiguration { Name = stepName };

        // Act
        var factory = _provider.CreateFactory<EvidenceHypothesisEvaluation>(config);

        // Assert
        Assert.NotNull(factory);
        Assert.IsType<EvidenceHypothesisEvaluationOrchestrationFactory>(factory);
    }

    #endregion

    #region CreateFactory - Error Handling Tests

    [Fact]
    public void CreateFactory_UnknownStepName_ThrowsInvalidOperationException()
    {
        // Arrange
        var config = new ACHStepConfiguration { Name = "Unknown Step" };

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() =>
            _provider.CreateFactory<List<Hypothesis>>(config));

        Assert.Contains("Unknown ACH step name", ex.Message);
    }

    [Fact]
    public void CreateFactory_TypeMismatch_HypothesisBrainstorming_ThrowsInvalidOperationException()
    {
        // Arrange
        var config = new ACHStepConfiguration { Name = "hypothesis brainstorming" };

        // Act & Assert
        // Requesting List<Evidence> but factory produces List<Hypothesis>
        var ex = Assert.Throws<InvalidOperationException>(() =>
            _provider.CreateFactory<List<Evidence>>(config));

        Assert.Contains("Type mismatch", ex.Message);
    }

    [Fact]
    public void CreateFactory_TypeMismatch_EvidenceExtraction_ThrowsInvalidOperationException()
    {
        // Arrange
        var config = new ACHStepConfiguration { Name = "evidence extraction" };

        // Act & Assert
        // Requesting List<Hypothesis> but factory produces List<Evidence>
        var ex = Assert.Throws<InvalidOperationException>(() =>
            _provider.CreateFactory<List<Hypothesis>>(config));

        Assert.Contains("Type mismatch", ex.Message);
    }

    [Fact]
    public void CreateFactory_TypeMismatch_EvidenceEvaluation_ThrowsInvalidOperationException()
    {
        // Arrange
        var config = new ACHStepConfiguration { Name = "evidence hypothesis evaluation" };

        // Act & Assert
        // Requesting List<Hypothesis> but factory produces EvidenceHypothesisEvaluation
        var ex = Assert.Throws<InvalidOperationException>(() =>
            _provider.CreateFactory<List<Hypothesis>>(config));

        Assert.Contains("Type mismatch", ex.Message);
    }

    #endregion
}
