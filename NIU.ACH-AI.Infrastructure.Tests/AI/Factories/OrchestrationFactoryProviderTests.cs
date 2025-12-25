using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using NIU.ACH_AI.Application.Configuration;
using NIU.ACH_AI.Application.Interfaces;
using NIU.ACH_AI.Domain.Entities;
using NIU.ACH_AI.Infrastructure.AI.Factories;

namespace NIU.ACH_AI.Infrastructure.Tests.AI.Factories;

/// <summary>
/// Unit tests for OrchestrationFactoryProvider.
///
/// Testing Strategy:
/// -----------------
/// OrchestrationFactoryProvider is a CONCRETE class that maps ACH step names
/// to specific orchestration factories via its public CreateFactory method.
/// We test the public behavior and selection logic without inspecting internals.
///
/// What We Test:
/// 1. Hypothesis brainstorming mappings - Returns HypothesisBrainstormingOrchestrationFactory
/// 2. Hypothesis refinement mappings - Returns HypothesisRefinementOrchestrationFactory
/// 3. Evidence extraction mappings - Returns EvidenceExtractionOrchestrationFactory
/// 4. Evidence hypothesis evaluation mappings - Returns EvidenceHypothesisEvaluationOrchestrationFactory
/// 5. Unknown or empty names - Throws InvalidOperationException with helpful message
/// 6. Null configuration - Throws an exception due to invalid input
/// 7. Mismatched result type - Throws InvalidOperationException for type mismatch
/// </summary>
public class OrchestrationFactoryProviderTests
{
    private static OrchestrationFactoryProvider CreateProvider()
    {
        var agentService = Mock.Of<IAgentService>();
        var kernelBuilderService = Mock.Of<IKernelBuilderService>();
        var options = Options.Create(new OrchestrationSettings());

        var loggerFactoryMock = new Mock<ILoggerFactory>();
        loggerFactoryMock
            .Setup(f => f.CreateLogger(It.IsAny<string>()))
            .Returns(Mock.Of<ILogger>());

        var executorMock = new Mock<IOrchestrationExecutor>();
        executorMock
            .Setup(e => e.CreateAgentService(It.IsAny<ACHStepConfiguration>()))
            .Returns(agentService);
        executorMock
            .Setup(e => e.GetKernelBuilderService())
            .Returns(kernelBuilderService);
        executorMock
            .Setup(e => e.CreateOrchestrationOptions(It.IsAny<ACHStepConfiguration>()))
            .Returns(options);
        executorMock
            .Setup(e => e.GetLoggerFactory())
            .Returns(loggerFactoryMock.Object);

        return new OrchestrationFactoryProvider(executorMock.Object);
    }

    private static ACHStepConfiguration CreateStep(string name)
    {
        return new ACHStepConfiguration { Name = name };
    }

    /// <summary>
    /// Verifies that valid hypothesis brainstorming step names map to the brainstorming factory.
    /// </summary>
    [Theory]
    [InlineData("Hypothesis Brainstorming")]
    [InlineData("HypothesisBrainstorming")]
    [InlineData("HYPOTHESIS BRAINSTORMING")]
    public void CreateFactory_WithHypothesisBrainstormingName_ReturnsBrainstormingFactory(string stepName)
    {
        // Arrange
        var provider = CreateProvider();
        var stepConfiguration = CreateStep(stepName);

        // Act
        var factory = provider.CreateFactory<List<Hypothesis>>(stepConfiguration);

        // Assert
        Assert.IsType<HypothesisBrainstormingOrchestrationFactory>(factory);
    }

    /// <summary>
    /// Verifies that valid hypothesis refinement step names map to the refinement factory.
    /// </summary>
    [Theory]
    [InlineData("Hypothesis Evaluation")]
    [InlineData("HypothesisEvaluation")]
    [InlineData("Hypothesis Refinement")]
    [InlineData("HypothesisRefinement")]
    public void CreateFactory_WithHypothesisRefinementName_ReturnsRefinementFactory(string stepName)
    {
        // Arrange
        var provider = CreateProvider();
        var stepConfiguration = CreateStep(stepName);

        // Act
        var factory = provider.CreateFactory<List<Hypothesis>>(stepConfiguration);

        // Assert
        Assert.IsType<HypothesisRefinementOrchestrationFactory>(factory);
    }

    /// <summary>
    /// Verifies that valid evidence extraction step names map to the extraction factory.
    /// </summary>
    [Theory]
    [InlineData("Evidence Extraction")]
    [InlineData("EvidenceExtraction")]
    public void CreateFactory_WithEvidenceExtractionName_ReturnsExtractionFactory(string stepName)
    {
        // Arrange
        var provider = CreateProvider();
        var stepConfiguration = CreateStep(stepName);

        // Act
        var factory = provider.CreateFactory<List<Evidence>>(stepConfiguration);

        // Assert
        Assert.IsType<EvidenceExtractionOrchestrationFactory>(factory);
    }

    /// <summary>
    /// Verifies that valid evidence hypothesis evaluation step names map to the evaluation factory.
    /// </summary>
    [Theory]
    [InlineData("Evidence Hypothesis Evaluation")]
    [InlineData("EvidenceHypothesisEvaluation")]
    [InlineData("Evidence Evaluation")]
    [InlineData("EvidenceEvaluation")]
    public void CreateFactory_WithEvidenceHypothesisEvaluationName_ReturnsEvaluationFactory(string stepName)
    {
        // Arrange
        var provider = CreateProvider();
        var stepConfiguration = CreateStep(stepName);

        // Act
        var factory = provider.CreateFactory<List<EvidenceHypothesisEvaluation>>(stepConfiguration);

        // Assert
        Assert.IsType<EvidenceHypothesisEvaluationOrchestrationFactory>(factory);
    }

    /// <summary>
    /// Verifies that an unknown step name causes CreateFactory to throw an InvalidOperationException.
    /// </summary>
    [Fact]
    public void CreateFactory_WithUnknownName_ThrowsInvalidOperationException()
    {
        // Arrange
        var provider = CreateProvider();
        var stepConfiguration = CreateStep("Unknown Step");

        // Act
        var exception = Assert.Throws<InvalidOperationException>(() =>
            provider.CreateFactory<List<Hypothesis>>(stepConfiguration));

        // Assert
        Assert.Contains("Unknown ACH step name", exception.Message);
        Assert.Contains(stepConfiguration.Name, exception.Message);
    }

    /// <summary>
    /// Verifies that an empty step name causes CreateFactory to throw an InvalidOperationException.
    /// </summary>
    [Fact]
    public void CreateFactory_WithEmptyName_ThrowsInvalidOperationException()
    {
        // Arrange
        var provider = CreateProvider();
        var stepConfiguration = CreateStep(string.Empty);

        // Act
        var exception = Assert.Throws<InvalidOperationException>(() =>
            provider.CreateFactory<List<Hypothesis>>(stepConfiguration));

        // Assert
        Assert.Contains("Unknown ACH step name", exception.Message);
    }

    /// <summary>
    /// Verifies that a null step configuration causes CreateFactory to throw an exception.
    /// </summary>
    [Fact]
    public void CreateFactory_WithNullConfiguration_ThrowsException()
    {
        // Arrange
        var provider = CreateProvider();

        // Act & Assert
        Assert.ThrowsAny<Exception>(() => provider.CreateFactory<List<Hypothesis>>(null!));
    }

    /// <summary>
    /// Verifies that requesting a factory with the wrong TResult type throws InvalidOperationException.
    /// </summary>
    [Fact]
    public void CreateFactory_WithMismatchedResultType_ThrowsInvalidOperationException()
    {
        // Arrange
        var provider = CreateProvider();
        var stepConfiguration = CreateStep("Hypothesis Brainstorming");

        // Act
        var exception = Assert.Throws<InvalidOperationException>(() =>
            provider.CreateFactory<List<Evidence>>(stepConfiguration));

        // Assert
        Assert.Contains("Type mismatch", exception.Message);
    }
}
