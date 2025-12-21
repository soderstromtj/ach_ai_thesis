using Microsoft.Extensions.DependencyInjection;
using Moq;
using NIU.ACH_AI.Application.Interfaces;
using NIU.ACH_AI.Domain.Entities;
using NIU.ACH_AI.Domain.ValueObjects;
using NIU.ACH_AI.Infrastructure.AI.Services;

namespace NIU.ACH_AI.Infrastructure.Tests.AI.Services;

/// <summary>
/// Unit tests for OrchestrationFactorySelector.
///
/// Testing Strategy:
/// -----------------
/// OrchestrationFactorySelector selects the appropriate orchestration factory
/// based on the ACH step. It uses the service provider to resolve factory instances.
///
/// Key testing areas:
/// 1. Constructor - Null validation
/// 2. GetFactory - Factory selection for each ACH step
/// 3. GetResultType - Result type mapping for each ACH step
/// 4. Error handling - NotSupportedException for unsupported steps
/// </summary>
public class OrchestrationFactorySelectorTests
{
    #region Test Infrastructure

    private static OrchestrationFactorySelector CreateSelector(IServiceProvider serviceProvider)
    {
        return new OrchestrationFactorySelector(serviceProvider);
    }

    private static Mock<IServiceProvider> CreateMockServiceProvider()
    {
        return new Mock<IServiceProvider>();
    }

    #endregion

    #region Constructor Tests

    /// <summary>
    /// WHY: Verifies selector can be instantiated with valid service provider.
    /// </summary>
    [Fact]
    public void Constructor_WithValidServiceProvider_CreatesInstance()
    {
        // Arrange
        var serviceProviderMock = CreateMockServiceProvider();

        // Act
        var selector = CreateSelector(serviceProviderMock.Object);

        // Assert
        Assert.NotNull(selector);
    }

    /// <summary>
    /// WHY: Verifies null service provider throws ArgumentNullException.
    /// </summary>
    [Fact]
    public void Constructor_WithNullServiceProvider_ThrowsArgumentNullException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() =>
            new OrchestrationFactorySelector(null!));
        Assert.Equal("serviceProvider", exception.ParamName);
    }

    /// <summary>
    /// WHY: Verifies selector implements IOrchestrationFactorySelector interface.
    /// </summary>
    [Fact]
    public void Selector_ImplementsIOrchestrationFactorySelector()
    {
        // Arrange
        var serviceProviderMock = CreateMockServiceProvider();
        var selector = CreateSelector(serviceProviderMock.Object);

        // Assert
        Assert.IsAssignableFrom<IOrchestrationFactorySelector>(selector);
    }

    #endregion

    #region GetFactory - HypothesisBrainstorming Tests

    /// <summary>
    /// WHY: Verifies HypothesisBrainstorming step returns Hypothesis factory.
    /// </summary>
    [Fact]
    public void GetFactory_ForHypothesisBrainstorming_RequestsHypothesisFactory()
    {
        // Arrange
        var serviceProviderMock = CreateMockServiceProvider();
        var factoryMock = new Mock<IOrchestrationFactory<List<Hypothesis>>>();

        serviceProviderMock
            .Setup(sp => sp.GetService(typeof(IOrchestrationFactory<List<Hypothesis>>)))
            .Returns(factoryMock.Object);

        var selector = CreateSelector(serviceProviderMock.Object);

        // Act
        var factory = selector.GetFactory(ACHStep.HypothesisBrainstorming);

        // Assert
        Assert.NotNull(factory);
        Assert.Same(factoryMock.Object, factory);
    }

    /// <summary>
    /// WHY: Verifies HypothesisBrainstorming resolves correct service type.
    /// </summary>
    [Fact]
    public void GetFactory_ForHypothesisBrainstorming_ResolvesCorrectServiceType()
    {
        // Arrange
        var serviceProviderMock = CreateMockServiceProvider();
        Type? resolvedType = null;

        serviceProviderMock
            .Setup(sp => sp.GetService(It.IsAny<Type>()))
            .Callback<Type>(t => resolvedType = t)
            .Returns(Mock.Of<IOrchestrationFactory<List<Hypothesis>>>());

        var selector = CreateSelector(serviceProviderMock.Object);

        // Act
        selector.GetFactory(ACHStep.HypothesisBrainstorming);

        // Assert
        Assert.Equal(typeof(IOrchestrationFactory<List<Hypothesis>>), resolvedType);
    }

    #endregion

    #region GetFactory - HypothesisRefinementSelection Tests

    /// <summary>
    /// WHY: Verifies HypothesisRefinementSelection step returns Evidence factory.
    /// Note: The current implementation maps this to Evidence, which may be intentional
    /// for the refinement workflow.
    /// </summary>
    [Fact]
    public void GetFactory_ForHypothesisRefinementSelection_RequestsEvidenceFactory()
    {
        // Arrange
        var serviceProviderMock = CreateMockServiceProvider();
        var factoryMock = new Mock<IOrchestrationFactory<List<Evidence>>>();

        serviceProviderMock
            .Setup(sp => sp.GetService(typeof(IOrchestrationFactory<List<Evidence>>)))
            .Returns(factoryMock.Object);

        var selector = CreateSelector(serviceProviderMock.Object);

        // Act
        var factory = selector.GetFactory(ACHStep.HypothesisRefinementSelection);

        // Assert
        Assert.NotNull(factory);
        Assert.Same(factoryMock.Object, factory);
    }

    #endregion

    #region GetFactory - Unsupported Steps Tests

    /// <summary>
    /// WHY: Verifies EvidenceExtraction throws NotSupportedException.
    /// </summary>
    [Fact]
    public void GetFactory_ForEvidenceExtraction_ThrowsNotSupportedException()
    {
        // Arrange
        var serviceProviderMock = CreateMockServiceProvider();
        var selector = CreateSelector(serviceProviderMock.Object);

        // Act & Assert
        var exception = Assert.Throws<NotSupportedException>(() =>
            selector.GetFactory(ACHStep.EvidenceExtraction));
        Assert.Contains("EvidenceExtraction", exception.Message);
        Assert.Contains("not yet implemented", exception.Message);
    }

    /// <summary>
    /// WHY: Verifies EvidenceEvaluation throws NotSupportedException.
    /// </summary>
    [Fact]
    public void GetFactory_ForEvidenceEvaluation_ThrowsNotSupportedException()
    {
        // Arrange
        var serviceProviderMock = CreateMockServiceProvider();
        var selector = CreateSelector(serviceProviderMock.Object);

        // Act & Assert
        var exception = Assert.Throws<NotSupportedException>(() =>
            selector.GetFactory(ACHStep.EvidenceEvaluation));
        Assert.Contains("EvidenceEvaluation", exception.Message);
        Assert.Contains("not yet implemented", exception.Message);
    }

    /// <summary>
    /// WHY: Verifies unknown ACH step value throws NotSupportedException.
    /// </summary>
    [Fact]
    public void GetFactory_ForUnknownStep_ThrowsNotSupportedException()
    {
        // Arrange
        var serviceProviderMock = CreateMockServiceProvider();
        var selector = CreateSelector(serviceProviderMock.Object);
        var unknownStep = (ACHStep)999;

        // Act & Assert
        var exception = Assert.Throws<NotSupportedException>(() =>
            selector.GetFactory(unknownStep));
        Assert.Contains("Unknown ACH step", exception.Message);
    }

    #endregion

    #region GetResultType - HypothesisBrainstorming Tests

    /// <summary>
    /// WHY: Verifies HypothesisBrainstorming returns List<Hypothesis> type.
    /// </summary>
    [Fact]
    public void GetResultType_ForHypothesisBrainstorming_ReturnsHypothesisListType()
    {
        // Arrange
        var serviceProviderMock = CreateMockServiceProvider();
        var selector = CreateSelector(serviceProviderMock.Object);

        // Act
        var resultType = selector.GetResultType(ACHStep.HypothesisBrainstorming);

        // Assert
        Assert.Equal(typeof(List<Hypothesis>), resultType);
    }

    #endregion

    #region GetResultType - HypothesisRefinementSelection Tests

    /// <summary>
    /// WHY: Verifies HypothesisRefinementSelection returns List<Evidence> type.
    /// </summary>
    [Fact]
    public void GetResultType_ForHypothesisRefinementSelection_ReturnsEvidenceListType()
    {
        // Arrange
        var serviceProviderMock = CreateMockServiceProvider();
        var selector = CreateSelector(serviceProviderMock.Object);

        // Act
        var resultType = selector.GetResultType(ACHStep.HypothesisRefinementSelection);

        // Assert
        Assert.Equal(typeof(List<Evidence>), resultType);
    }

    #endregion

    #region GetResultType - Unsupported Steps Tests

    /// <summary>
    /// WHY: Verifies EvidenceExtraction throws NotSupportedException for GetResultType.
    /// </summary>
    [Fact]
    public void GetResultType_ForEvidenceExtraction_ThrowsNotSupportedException()
    {
        // Arrange
        var serviceProviderMock = CreateMockServiceProvider();
        var selector = CreateSelector(serviceProviderMock.Object);

        // Act & Assert
        var exception = Assert.Throws<NotSupportedException>(() =>
            selector.GetResultType(ACHStep.EvidenceExtraction));
        Assert.Contains("EvidenceExtraction", exception.Message);
    }

    /// <summary>
    /// WHY: Verifies EvidenceEvaluation throws NotSupportedException for GetResultType.
    /// </summary>
    [Fact]
    public void GetResultType_ForEvidenceEvaluation_ThrowsNotSupportedException()
    {
        // Arrange
        var serviceProviderMock = CreateMockServiceProvider();
        var selector = CreateSelector(serviceProviderMock.Object);

        // Act & Assert
        var exception = Assert.Throws<NotSupportedException>(() =>
            selector.GetResultType(ACHStep.EvidenceEvaluation));
        Assert.Contains("EvidenceEvaluation", exception.Message);
    }

    /// <summary>
    /// WHY: Verifies unknown ACH step value throws NotSupportedException for GetResultType.
    /// </summary>
    [Fact]
    public void GetResultType_ForUnknownStep_ThrowsNotSupportedException()
    {
        // Arrange
        var serviceProviderMock = CreateMockServiceProvider();
        var selector = CreateSelector(serviceProviderMock.Object);
        var unknownStep = (ACHStep)999;

        // Act & Assert
        var exception = Assert.Throws<NotSupportedException>(() =>
            selector.GetResultType(unknownStep));
        Assert.Contains("Unknown ACH step", exception.Message);
    }

    #endregion

    #region All ACH Steps Tests

    /// <summary>
    /// WHY: Documents the expected behavior for each ACH step.
    /// </summary>
    [Theory]
    [InlineData(ACHStep.HypothesisBrainstorming, false)]
    [InlineData(ACHStep.HypothesisRefinementSelection, false)]
    [InlineData(ACHStep.EvidenceExtraction, true)]
    [InlineData(ACHStep.EvidenceEvaluation, true)]
    public void GetFactory_ForACHStep_BehavesAsExpected(ACHStep step, bool shouldThrow)
    {
        // Arrange
        var serviceProviderMock = CreateMockServiceProvider();
        serviceProviderMock
            .Setup(sp => sp.GetService(It.IsAny<Type>()))
            .Returns(Mock.Of<IOrchestrationFactory<List<Hypothesis>>>());

        var selector = CreateSelector(serviceProviderMock.Object);

        // Act & Assert
        if (shouldThrow)
        {
            Assert.Throws<NotSupportedException>(() => selector.GetFactory(step));
        }
        else
        {
            var factory = selector.GetFactory(step);
            Assert.NotNull(factory);
        }
    }

    /// <summary>
    /// WHY: Documents the result types for each supported ACH step.
    /// </summary>
    [Theory]
    [InlineData(ACHStep.HypothesisBrainstorming, typeof(List<Hypothesis>))]
    [InlineData(ACHStep.HypothesisRefinementSelection, typeof(List<Evidence>))]
    public void GetResultType_ForSupportedStep_ReturnsCorrectType(ACHStep step, Type expectedType)
    {
        // Arrange
        var serviceProviderMock = CreateMockServiceProvider();
        var selector = CreateSelector(serviceProviderMock.Object);

        // Act
        var resultType = selector.GetResultType(step);

        // Assert
        Assert.Equal(expectedType, resultType);
    }

    #endregion

    #region Service Resolution Tests

    /// <summary>
    /// WHY: Verifies exception when service is not registered.
    /// </summary>
    [Fact]
    public void GetFactory_WhenServiceNotRegistered_ThrowsInvalidOperationException()
    {
        // Arrange
        var serviceProviderMock = CreateMockServiceProvider();
        serviceProviderMock
            .Setup(sp => sp.GetService(typeof(IOrchestrationFactory<List<Hypothesis>>)))
            .Returns(null!);

        var selector = CreateSelector(serviceProviderMock.Object);

        // Act & Assert - GetRequiredService throws when service is null
        Assert.Throws<InvalidOperationException>(() =>
            selector.GetFactory(ACHStep.HypothesisBrainstorming));
    }

    #endregion
}
