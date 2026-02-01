using Microsoft.EntityFrameworkCore;
using Moq;

using NIU.ACH_AI.Application.Interfaces;
using DomainEntities = NIU.ACH_AI.Domain.Entities;
using NIU.ACH_AI.Infrastructure.Persistence.Models;
using NIU.ACH_AI.Infrastructure.Persistence.Repositories;
using NIU.ACH_AI.Infrastructure.Persistence.Services;

namespace NIU.ACH_AI.Infrastructure.Tests.Persistence.Services;

public class WorkflowResultPersistenceTests
{
    private readonly Mock<AchAIDbContext> _contextMock;
    private readonly Mock<IHypothesisRepository> _hypothesisRepositoryMock;
    private readonly Mock<IEvidenceRepository> _evidenceRepositoryMock;
    private readonly Mock<IEvidenceHypothesisEvaluationRepository> _evaluationRepositoryMock;
    private readonly WorkflowResultPersistence _persistence;

    public WorkflowResultPersistenceTests()
    {
        // Setup DbContext Mock (Just enough to pass constructor validation)
        var options = new DbContextOptionsBuilder<AchAIDbContext>().Options;
        _contextMock = new Mock<AchAIDbContext>(options);

        _hypothesisRepositoryMock = new Mock<IHypothesisRepository>();
        _evidenceRepositoryMock = new Mock<IEvidenceRepository>();
        _evaluationRepositoryMock = new Mock<IEvidenceHypothesisEvaluationRepository>();

        _persistence = new WorkflowResultPersistence(
            _contextMock.Object,
            _hypothesisRepositoryMock.Object,
            _evidenceRepositoryMock.Object,
            _evaluationRepositoryMock.Object);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithValidArgs_CreatesInstance()
    {
        Assert.NotNull(_persistence);
    }

    [Fact]
    public void Constructor_WithNullArgs_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new WorkflowResultPersistence(null!, _hypothesisRepositoryMock.Object, _evidenceRepositoryMock.Object, _evaluationRepositoryMock.Object));
        Assert.Throws<ArgumentNullException>(() => new WorkflowResultPersistence(_contextMock.Object, null!, _evidenceRepositoryMock.Object, _evaluationRepositoryMock.Object));
        Assert.Throws<ArgumentNullException>(() => new WorkflowResultPersistence(_contextMock.Object, _hypothesisRepositoryMock.Object, null!, _evaluationRepositoryMock.Object));
        Assert.Throws<ArgumentNullException>(() => new WorkflowResultPersistence(_contextMock.Object, _hypothesisRepositoryMock.Object, _evidenceRepositoryMock.Object, null!));
    }

    #endregion

    #region SaveHypothesesAsync Tests

    [Fact]
    public async Task SaveHypothesesAsync_WithEmptyStepId_ThrowsArgumentException()
    {
        await Assert.ThrowsAsync<ArgumentException>(() => 
            _persistence.SaveHypothesesAsync(Guid.Empty, new List<DomainEntities.Hypothesis>(), false));
    }

    [Fact]
    public async Task SaveHypothesesAsync_DelegatesToRepository()
    {
        // Arrange
        var stepId = Guid.NewGuid();
        var hypotheses = new List<DomainEntities.Hypothesis> { new DomainEntities.Hypothesis { ShortTitle = "H1" } };
        var isRefined = true;
        
        _hypothesisRepositoryMock.Setup(r => r.SaveBatchAsync(hypotheses, stepId, isRefined, It.IsAny<CancellationToken>()))
            .ReturnsAsync(hypotheses);

        // Act
        var result = await _persistence.SaveHypothesesAsync(stepId, hypotheses, isRefined);

        // Assert
        Assert.Same(hypotheses, result);
        _hypothesisRepositoryMock.Verify(r => r.SaveBatchAsync(hypotheses, stepId, isRefined, It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region SaveEvidenceAsync Tests

    [Fact]
    public async Task SaveEvidenceAsync_WithEmptyStepId_ThrowsArgumentException()
    {
        await Assert.ThrowsAsync<ArgumentException>(() => 
            _persistence.SaveEvidenceAsync(Guid.Empty, new List<DomainEntities.Evidence>()));
    }

    [Fact]
    public async Task SaveEvidenceAsync_DelegatesToRepository()
    {
        // Arrange
        var stepId = Guid.NewGuid();
        var evidence = new List<DomainEntities.Evidence> { new DomainEntities.Evidence { Claim = "E1" } };
        
        _evidenceRepositoryMock.Setup(r => r.SaveBatchAsync(evidence, stepId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(evidence);

        // Act
        var result = await _persistence.SaveEvidenceAsync(stepId, evidence);

        // Assert
        Assert.Same(evidence, result);
        _evidenceRepositoryMock.Verify(r => r.SaveBatchAsync(evidence, stepId, It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region SaveEvaluationAsync Tests

    [Fact]
    public async Task SaveEvaluationAsync_WithEmptyIds_ThrowsArgumentException()
    {
        var eval = new DomainEntities.EvidenceHypothesisEvaluation();
        
        await Assert.ThrowsAsync<ArgumentException>(() => 
            _persistence.SaveEvaluationAsync(Guid.Empty, eval, Guid.NewGuid(), Guid.NewGuid()));

        await Assert.ThrowsAsync<ArgumentException>(() => 
            _persistence.SaveEvaluationAsync(Guid.NewGuid(), eval, Guid.Empty, Guid.NewGuid()));

        await Assert.ThrowsAsync<ArgumentException>(() => 
            _persistence.SaveEvaluationAsync(Guid.NewGuid(), eval, Guid.NewGuid(), Guid.Empty));
    }

    [Fact]
    public async Task SaveEvaluationAsync_DelegatesToRepositoryWithMaps()
    {
        // Arrange
        var stepId = Guid.NewGuid();
        var hId = Guid.NewGuid();
        var eId = Guid.NewGuid();
        
        var hypothesis = new DomainEntities.Hypothesis { HypothesisId = hId, ShortTitle = "Hypothesis1" };
        var evidence = new DomainEntities.Evidence { EvidenceId = eId, Claim = "Evidence1" };
        var evaluation = new DomainEntities.EvidenceHypothesisEvaluation { Hypothesis = hypothesis, Evidence = evidence };

        // Act
        await _persistence.SaveEvaluationAsync(stepId, evaluation, Guid.NewGuid(), Guid.NewGuid());

        // Assert
        _evaluationRepositoryMock.Verify(r => r.SaveBatchAsync(
            It.Is<IEnumerable<DomainEntities.EvidenceHypothesisEvaluation>>(l => l.Contains(evaluation)),
            stepId,
            It.Is<Dictionary<string, Guid>>(d => d[hypothesis.ShortTitle] == hId),
            It.Is<Dictionary<string, Guid>>(d => d[evidence.Claim] == eId),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion
}
