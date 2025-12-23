using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NIU.ACH_AI.Domain.ValueObjects;
using NIU.ACH_AI.Infrastructure.Persistence.Models;
using NIU.ACH_AI.Infrastructure.Persistence.Repositories;
using DomainEntity = NIU.ACH_AI.Domain.Entities;

namespace NIU.ACH_AI.Infrastructure.Persistence.Tests.Repositories;

/// <summary>
/// Unit tests for EvidenceHypothesisEvaluationRepository following FIRST principles.
/// Uses in-memory database for fast, isolated tests.
/// </summary>
public class EvidenceHypothesisEvaluationRepositoryTests : IDisposable
{
    private readonly AchAIDbContext _context;
    private readonly EvidenceHypothesisEvaluationRepository _repository;

    public EvidenceHypothesisEvaluationRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<AchAIDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new AchAIDbContext(options);
        _repository = new EvidenceHypothesisEvaluationRepository(_context);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    #region Constructor Tests

    /// <summary>

    /// Verifies that the constructor throws ArgumentNullException when passed a null null context throws argument null exception.

    /// </summary>

    [Fact]
    public void Constructor_WithNullContext_ThrowsArgumentNullException()
    {
        // Arrange, Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() => 
            new EvidenceHypothesisEvaluationRepository(null!));
        exception.ParamName.Should().Be("context");
    }

    /// <summary>

    /// Verifies that the constructor successfully creates an instance with valid valid context creates instance.

    /// </summary>

    [Fact]
    public void Constructor_WithValidContext_CreatesInstance()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<AchAIDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        var context = new AchAIDbContext(options);

        // Act
        var repository = new EvidenceHypothesisEvaluationRepository(context);

        // Assert
        repository.Should().NotBeNull();
        
        // Cleanup
        context.Dispose();
    }

    #endregion

    #region SaveBatchAsync Tests

    /// <summary>

    /// Verifies that Save successfully saves batch async with valid data.

    /// </summary>

    [Fact]
    public async Task SaveBatchAsync_WithValidEvaluations_SavesAllEntities()
    {
        // Arrange
        var stepExecutionId = Guid.NewGuid();
        var evaluations = new List<DomainEntity.EvidenceHypothesisEvaluation>
        {
            new DomainEntity.EvidenceHypothesisEvaluation
            {
                Hypothesis = new DomainEntity.Hypothesis { ShortTitle = "H1", HypothesisText = "Text1" },
                Evidence = new DomainEntity.Evidence { Claim = "E1", Type = Domain.ValueObjects.EvidenceType.Fact },
                Score = Domain.ValueObjects.EvaluationScore.Consistent,
                ScoreRationale = "Rationale 1",
                ConfidenceLevel = 0.8m,
                ConfidenceRationale = "Confidence 1"
            },
            new DomainEntity.EvidenceHypothesisEvaluation
            {
                Hypothesis = new DomainEntity.Hypothesis { ShortTitle = "H2", HypothesisText = "Text2" },
                Evidence = new DomainEntity.Evidence { Claim = "E2", Type = Domain.ValueObjects.EvidenceType.Assumption },
                Score = Domain.ValueObjects.EvaluationScore.Inconsistent,
                ScoreRationale = "Rationale 2",
                ConfidenceLevel = 0.6m,
                ConfidenceRationale = "Confidence 2"
            }
        };

        var hypothesisIdMap = new Dictionary<string, Guid>
        {
            { "H1", Guid.NewGuid() },
            { "H2", Guid.NewGuid() }
        };

        var evidenceIdMap = new Dictionary<string, Guid>
        {
            { "E1", Guid.NewGuid() },
            { "E2", Guid.NewGuid() }
        };

        // Act
        await _repository.SaveBatchAsync(evaluations, stepExecutionId, hypothesisIdMap, evidenceIdMap);

        // Assert
        var savedEvaluations = await _context.EvidenceHypothesisEvaluations.ToListAsync();
        savedEvaluations.Should().HaveCount(2);
        savedEvaluations.All(e => e.StepExecutionId == stepExecutionId).Should().BeTrue();
    }

    /// <summary>

    /// Verifies that Save handles null input gracefully without throwing exceptions.

    /// </summary>

    [Fact]
    public async Task SaveBatchAsync_WithNullEvaluationList_DoesNothing()
    {
        // Arrange
        var stepExecutionId = Guid.NewGuid();
        var hypothesisIdMap = new Dictionary<string, Guid>();
        var evidenceIdMap = new Dictionary<string, Guid>();

        // Act
        await _repository.SaveBatchAsync(null!, stepExecutionId, hypothesisIdMap, evidenceIdMap);

        // Assert
        var savedEvaluations = await _context.EvidenceHypothesisEvaluations.ToListAsync();
        savedEvaluations.Should().BeEmpty();
    }

    /// <summary>

    /// Verifies that Save handles empty collections gracefully without saving anything.

    /// </summary>

    [Fact]
    public async Task SaveBatchAsync_WithEmptyEvaluationList_DoesNothing()
    {
        // Arrange
        var stepExecutionId = Guid.NewGuid();
        var evaluations = new List<DomainEntity.EvidenceHypothesisEvaluation>();
        var hypothesisIdMap = new Dictionary<string, Guid>();
        var evidenceIdMap = new Dictionary<string, Guid>();

        // Act
        await _repository.SaveBatchAsync(evaluations, stepExecutionId, hypothesisIdMap, evidenceIdMap);

        // Assert
        var savedEvaluations = await _context.EvidenceHypothesisEvaluations.ToListAsync();
        savedEvaluations.Should().BeEmpty();
    }

    /// <summary>

    /// Verifies that Save throws an exception when with missing hypothesis in map throws invalid operation exception is missing from the ID map.

    /// </summary>

    [Fact]
    public async Task SaveBatchAsync_WithMissingHypothesisInMap_ThrowsInvalidOperationException()
    {
        // Arrange
        var stepExecutionId = Guid.NewGuid();
        var evaluations = new List<DomainEntity.EvidenceHypothesisEvaluation>
        {
            new DomainEntity.EvidenceHypothesisEvaluation
            {
                Hypothesis = new DomainEntity.Hypothesis { ShortTitle = "H1", HypothesisText = "Text1" },
                Evidence = new DomainEntity.Evidence { Claim = "E1", Type = Domain.ValueObjects.EvidenceType.Fact },
                Score = Domain.ValueObjects.EvaluationScore.Consistent,
                ScoreRationale = "Test",
                ConfidenceLevel = 0.5m,
                ConfidenceRationale = "Test"
            }
        };

        var hypothesisIdMap = new Dictionary<string, Guid>(); // Empty - missing H1
        var evidenceIdMap = new Dictionary<string, Guid> { { "E1", Guid.NewGuid() } };

        // Act & Assert
        await _repository.Invoking(r => r.SaveBatchAsync(evaluations, stepExecutionId, hypothesisIdMap, evidenceIdMap))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Hypothesis 'H1' not found*");
    }

    /// <summary>

    /// Verifies that Save throws an exception when with missing evidence in map throws invalid operation exception is missing from the ID map.

    /// </summary>

    [Fact]
    public async Task SaveBatchAsync_WithMissingEvidenceInMap_ThrowsInvalidOperationException()
    {
        // Arrange
        var stepExecutionId = Guid.NewGuid();
        var evaluations = new List<DomainEntity.EvidenceHypothesisEvaluation>
        {
            new DomainEntity.EvidenceHypothesisEvaluation
            {
                Hypothesis = new DomainEntity.Hypothesis { ShortTitle = "H1", HypothesisText = "Text1" },
                Evidence = new DomainEntity.Evidence { Claim = "E1", Type = Domain.ValueObjects.EvidenceType.Fact },
                Score = Domain.ValueObjects.EvaluationScore.Consistent,
                ScoreRationale = "Test",
                ConfidenceLevel = 0.5m,
                ConfidenceRationale = "Test"
            }
        };

        var hypothesisIdMap = new Dictionary<string, Guid> { { "H1", Guid.NewGuid() } };
        var evidenceIdMap = new Dictionary<string, Guid>(); // Empty - missing E1

        // Act & Assert
        await _repository.Invoking(r => r.SaveBatchAsync(evaluations, stepExecutionId, hypothesisIdMap, evidenceIdMap))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Evidence 'E1' not found*");
    }

    /// <summary>

    /// Verifies that save batch async with single evaluation saves successfully.

    /// </summary>

    [Fact]
    public async Task SaveBatchAsync_WithSingleEvaluation_SavesSuccessfully()
    {
        // Arrange
        var stepExecutionId = Guid.NewGuid();
        var evaluations = new List<DomainEntity.EvidenceHypothesisEvaluation>
        {
            new DomainEntity.EvidenceHypothesisEvaluation
            {
                Hypothesis = new DomainEntity.Hypothesis { ShortTitle = "H1", HypothesisText = "Text1" },
                Evidence = new DomainEntity.Evidence { Claim = "E1", Type = Domain.ValueObjects.EvidenceType.Fact },
                Score = Domain.ValueObjects.EvaluationScore.Neutral,
                ScoreRationale = "Test rationale",
                ConfidenceLevel = 0.5m,
                ConfidenceRationale = "Test confidence"
            }
        };

        var hypothesisIdMap = new Dictionary<string, Guid> { { "H1", Guid.NewGuid() } };
        var evidenceIdMap = new Dictionary<string, Guid> { { "E1", Guid.NewGuid() } };

        // Act
        await _repository.SaveBatchAsync(evaluations, stepExecutionId, hypothesisIdMap, evidenceIdMap);

        // Assert
        var savedEvaluations = await _context.EvidenceHypothesisEvaluations.ToListAsync();
        savedEvaluations.Should().HaveCount(1);
    }

    #endregion

    #region GetByStepExecutionIdAsync Tests

    /// <summary>

    /// Verifies that Get returns the correct by step when it exists in the database.

    /// </summary>

    [Fact]
    public async Task GetByStepExecutionIdAsync_WithExistingEvaluations_ReturnsAllMatching()
    {
        // Arrange
        var stepExecutionId = Guid.NewGuid();
        var otherStepExecutionId = Guid.NewGuid();
        
        var hypothesis1 = new Hypothesis
        {
            HypothesisId = Guid.NewGuid(),
            StepExecutionId = stepExecutionId,
            ShortTitle = "H1",
            HypothesisText = "Text1",
            IsRefined = false,
            CreatedAt = DateTime.UtcNow
        };

        var hypothesis2 = new Hypothesis
        {
            HypothesisId = Guid.NewGuid(),
            StepExecutionId = stepExecutionId,
            ShortTitle = "H2",
            HypothesisText = "Text2",
            IsRefined = false,
            CreatedAt = DateTime.UtcNow
        };

        var evidence1 = new Evidence
        {
            EvidenceId = Guid.NewGuid(),
            StepExecutionId = stepExecutionId,
            Claim = "E1",
            EvidenceTypeId = (int)Domain.ValueObjects.EvidenceType.Fact,
            CreatedAt = DateTime.UtcNow
        };

        var evidence2 = new Evidence
        {
            EvidenceId = Guid.NewGuid(),
            StepExecutionId = stepExecutionId,
            Claim = "E2",
            EvidenceTypeId = (int)Domain.ValueObjects.EvidenceType.Assumption,
            CreatedAt = DateTime.UtcNow
        };

        await _context.Hypotheses.AddRangeAsync(hypothesis1, hypothesis2);
        await _context.Evidences.AddRangeAsync(evidence1, evidence2);
        await _context.SaveChangesAsync();

        await _context.EvidenceHypothesisEvaluations.AddRangeAsync(
            new EvidenceHypothesisEvaluation
            {
                EvidenceHypothesisEvaluationId = Guid.NewGuid(),
                StepExecutionId = stepExecutionId,
                HypothesisId = hypothesis1.HypothesisId,
                EvidenceId = evidence1.EvidenceId,
                EvaluationScoreId = (int)Domain.ValueObjects.EvaluationScore.Consistent,
                Rationale = "Rationale 1",
                ConfidenceScore = 0.8m,
                ConfidenceRationale = "Confidence 1",
                CreatedAt = DateTime.UtcNow
            },
            new EvidenceHypothesisEvaluation
            {
                EvidenceHypothesisEvaluationId = Guid.NewGuid(),
                StepExecutionId = stepExecutionId,
                HypothesisId = hypothesis2.HypothesisId,
                EvidenceId = evidence2.EvidenceId,
                EvaluationScoreId = (int)Domain.ValueObjects.EvaluationScore.Inconsistent,
                Rationale = "Rationale 2",
                ConfidenceScore = 0.6m,
                ConfidenceRationale = "Confidence 2",
                CreatedAt = DateTime.UtcNow
            },
            new EvidenceHypothesisEvaluation
            {
                EvidenceHypothesisEvaluationId = Guid.NewGuid(),
                StepExecutionId = otherStepExecutionId,
                HypothesisId = hypothesis1.HypothesisId,
                EvidenceId = evidence1.EvidenceId,
                EvaluationScoreId = (int)Domain.ValueObjects.EvaluationScore.Neutral,
                Rationale = "Other rationale",
                CreatedAt = DateTime.UtcNow
            }
        );
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByStepExecutionIdAsync(stepExecutionId);

        // Assert
        result.Should().HaveCount(2);
        result.Should().AllSatisfy(e => e.Hypothesis.Should().NotBeNull());
        result.Should().AllSatisfy(e => e.Evidence.Should().NotBeNull());
    }

    /// <summary>

    /// Verifies that get by step execution id async with no matching evaluations returns empty list.

    /// </summary>

    [Fact]
    public async Task GetByStepExecutionIdAsync_WithNoMatchingEvaluations_ReturnsEmptyList()
    {
        // Arrange
        var stepExecutionId = Guid.NewGuid();

        // Act
        var result = await _repository.GetByStepExecutionIdAsync(stepExecutionId);

        // Assert
        result.Should().BeEmpty();
    }

    /// <summary>

    /// Verifies that Get eagerly loads related navigation properties.

    /// </summary>

    [Fact]
    public async Task GetByStepExecutionIdAsync_IncludesNavigationProperties()
    {
        // Arrange
        var stepExecutionId = Guid.NewGuid();
        
        var hypothesis = new Hypothesis
        {
            HypothesisId = Guid.NewGuid(),
            StepExecutionId = stepExecutionId,
            ShortTitle = "Test Hypothesis",
            HypothesisText = "Test Text",
            IsRefined = false,
            CreatedAt = DateTime.UtcNow
        };

        var evidence = new Evidence
        {
            EvidenceId = Guid.NewGuid(),
            StepExecutionId = stepExecutionId,
            Claim = "Test Evidence",
            EvidenceTypeId = (int)Domain.ValueObjects.EvidenceType.Fact,
            CreatedAt = DateTime.UtcNow
        };

        await _context.Hypotheses.AddAsync(hypothesis);
        await _context.Evidences.AddAsync(evidence);
        await _context.SaveChangesAsync();

        await _context.EvidenceHypothesisEvaluations.AddAsync(
            new EvidenceHypothesisEvaluation
            {
                EvidenceHypothesisEvaluationId = Guid.NewGuid(),
                StepExecutionId = stepExecutionId,
                HypothesisId = hypothesis.HypothesisId,
                EvidenceId = evidence.EvidenceId,
                EvaluationScoreId = (int)Domain.ValueObjects.EvaluationScore.Consistent,
                CreatedAt = DateTime.UtcNow
            }
        );
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByStepExecutionIdAsync(stepExecutionId);

        // Assert
        result.Should().HaveCount(1);
        result[0].Hypothesis.ShortTitle.Should().Be("Test Hypothesis");
        result[0].Evidence.Claim.Should().Be("Test Evidence");
    }

    /// <summary>

    /// Verifies that Get uses AsNoTracking to prevent EF Core from tracking the entities.

    /// </summary>

    [Fact]
    public async Task GetByStepExecutionIdAsync_DoesNotTrackEntities()
    {
        // Arrange
        var stepExecutionId = Guid.NewGuid();
        
        var hypothesis = new Hypothesis
        {
            HypothesisId = Guid.NewGuid(),
            StepExecutionId = stepExecutionId,
            ShortTitle = "Test",
            HypothesisText = "Test",
            IsRefined = false,
            CreatedAt = DateTime.UtcNow
        };

        var evidence = new Evidence
        {
            EvidenceId = Guid.NewGuid(),
            StepExecutionId = stepExecutionId,
            Claim = "Test",
            EvidenceTypeId = (int)Domain.ValueObjects.EvidenceType.Fact,
            CreatedAt = DateTime.UtcNow
        };

        await _context.Hypotheses.AddAsync(hypothesis);
        await _context.Evidences.AddAsync(evidence);
        await _context.SaveChangesAsync();

        await _context.EvidenceHypothesisEvaluations.AddAsync(
            new EvidenceHypothesisEvaluation
            {
                EvidenceHypothesisEvaluationId = Guid.NewGuid(),
                StepExecutionId = stepExecutionId,
                HypothesisId = hypothesis.HypothesisId,
                EvidenceId = evidence.EvidenceId,
                EvaluationScoreId = (int)Domain.ValueObjects.EvaluationScore.Consistent,
                CreatedAt = DateTime.UtcNow
            }
        );
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByStepExecutionIdAsync(stepExecutionId);

        // Assert
        var trackedEntities = _context.ChangeTracker.Entries<EvidenceHypothesisEvaluation>().Count();
        trackedEntities.Should().Be(0);
    }

    #endregion

    #region GetByHypothesisIdAsync Tests

    /// <summary>

    /// Verifies that Get returns the correct by hypothesis when it exists in the database.

    /// </summary>

    [Fact]
    public async Task GetByHypothesisIdAsync_WithExistingHypothesis_ReturnsAllMatchingEvaluations()
    {
        // Arrange
        var hypothesis1 = new Hypothesis
        {
            HypothesisId = Guid.NewGuid(),
            StepExecutionId = Guid.NewGuid(),
            ShortTitle = "H1",
            HypothesisText = "Text1",
            IsRefined = false,
            CreatedAt = DateTime.UtcNow
        };

        var hypothesis2 = new Hypothesis
        {
            HypothesisId = Guid.NewGuid(),
            StepExecutionId = Guid.NewGuid(),
            ShortTitle = "H2",
            HypothesisText = "Text2",
            IsRefined = false,
            CreatedAt = DateTime.UtcNow
        };

        var evidence1 = new Evidence
        {
            EvidenceId = Guid.NewGuid(),
            StepExecutionId = Guid.NewGuid(),
            Claim = "E1",
            EvidenceTypeId = (int)Domain.ValueObjects.EvidenceType.Fact,
            CreatedAt = DateTime.UtcNow
        };

        var evidence2 = new Evidence
        {
            EvidenceId = Guid.NewGuid(),
            StepExecutionId = Guid.NewGuid(),
            Claim = "E2",
            EvidenceTypeId = (int)Domain.ValueObjects.EvidenceType.Assumption,
            CreatedAt = DateTime.UtcNow
        };

        await _context.Hypotheses.AddRangeAsync(hypothesis1, hypothesis2);
        await _context.Evidences.AddRangeAsync(evidence1, evidence2);
        await _context.SaveChangesAsync();

        await _context.EvidenceHypothesisEvaluations.AddRangeAsync(
            new EvidenceHypothesisEvaluation
            {
                EvidenceHypothesisEvaluationId = Guid.NewGuid(),
                StepExecutionId = Guid.NewGuid(),
                HypothesisId = hypothesis1.HypothesisId,
                EvidenceId = evidence1.EvidenceId,
                EvaluationScoreId = (int)Domain.ValueObjects.EvaluationScore.Consistent,
                CreatedAt = DateTime.UtcNow
            },
            new EvidenceHypothesisEvaluation
            {
                EvidenceHypothesisEvaluationId = Guid.NewGuid(),
                StepExecutionId = Guid.NewGuid(),
                HypothesisId = hypothesis1.HypothesisId,
                EvidenceId = evidence2.EvidenceId,
                EvaluationScoreId = (int)Domain.ValueObjects.EvaluationScore.Inconsistent,
                CreatedAt = DateTime.UtcNow
            },
            new EvidenceHypothesisEvaluation
            {
                EvidenceHypothesisEvaluationId = Guid.NewGuid(),
                StepExecutionId = Guid.NewGuid(),
                HypothesisId = hypothesis2.HypothesisId,
                EvidenceId = evidence1.EvidenceId,
                EvaluationScoreId = (int)Domain.ValueObjects.EvaluationScore.Neutral,
                CreatedAt = DateTime.UtcNow
            }
        );
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByHypothesisIdAsync(hypothesis1.HypothesisId);

        // Assert
        result.Should().HaveCount(2);
        result.Should().AllSatisfy(e => e.Hypothesis.ShortTitle.Should().Be("H1"));
    }

    /// <summary>

    /// Verifies that get by hypothesis id async with no matching evaluations returns empty list.

    /// </summary>

    [Fact]
    public async Task GetByHypothesisIdAsync_WithNoMatchingEvaluations_ReturnsEmptyList()
    {
        // Arrange
        var hypothesisId = Guid.NewGuid();

        // Act
        var result = await _repository.GetByHypothesisIdAsync(hypothesisId);

        // Assert
        result.Should().BeEmpty();
    }

    /// <summary>

    /// Verifies that Get uses AsNoTracking to prevent EF Core from tracking the entities.

    /// </summary>

    [Fact]
    public async Task GetByHypothesisIdAsync_DoesNotTrackEntities()
    {
        // Arrange
        var hypothesis = new Hypothesis
        {
            HypothesisId = Guid.NewGuid(),
            StepExecutionId = Guid.NewGuid(),
            ShortTitle = "Test",
            HypothesisText = "Test",
            IsRefined = false,
            CreatedAt = DateTime.UtcNow
        };

        var evidence = new Evidence
        {
            EvidenceId = Guid.NewGuid(),
            StepExecutionId = Guid.NewGuid(),
            Claim = "Test",
            EvidenceTypeId = (int)Domain.ValueObjects.EvidenceType.Fact,
            CreatedAt = DateTime.UtcNow
        };

        await _context.Hypotheses.AddAsync(hypothesis);
        await _context.Evidences.AddAsync(evidence);
        await _context.SaveChangesAsync();

        await _context.EvidenceHypothesisEvaluations.AddAsync(
            new EvidenceHypothesisEvaluation
            {
                EvidenceHypothesisEvaluationId = Guid.NewGuid(),
                StepExecutionId = Guid.NewGuid(),
                HypothesisId = hypothesis.HypothesisId,
                EvidenceId = evidence.EvidenceId,
                EvaluationScoreId = (int)Domain.ValueObjects.EvaluationScore.Consistent,
                CreatedAt = DateTime.UtcNow
            }
        );
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByHypothesisIdAsync(hypothesis.HypothesisId);

        // Assert
        var trackedEntities = _context.ChangeTracker.Entries<EvidenceHypothesisEvaluation>().Count();
        trackedEntities.Should().Be(0);
    }

    #endregion

    #region GetByEvidenceIdAsync Tests

    /// <summary>

    /// Verifies that Get returns the correct by evidence when it exists in the database.

    /// </summary>

    [Fact]
    public async Task GetByEvidenceIdAsync_WithExistingEvidence_ReturnsAllMatchingEvaluations()
    {
        // Arrange
        var hypothesis1 = new Hypothesis
        {
            HypothesisId = Guid.NewGuid(),
            StepExecutionId = Guid.NewGuid(),
            ShortTitle = "H1",
            HypothesisText = "Text1",
            IsRefined = false,
            CreatedAt = DateTime.UtcNow
        };

        var hypothesis2 = new Hypothesis
        {
            HypothesisId = Guid.NewGuid(),
            StepExecutionId = Guid.NewGuid(),
            ShortTitle = "H2",
            HypothesisText = "Text2",
            IsRefined = false,
            CreatedAt = DateTime.UtcNow
        };

        var evidence1 = new Evidence
        {
            EvidenceId = Guid.NewGuid(),
            StepExecutionId = Guid.NewGuid(),
            Claim = "E1",
            EvidenceTypeId = (int)Domain.ValueObjects.EvidenceType.Fact,
            CreatedAt = DateTime.UtcNow
        };

        var evidence2 = new Evidence
        {
            EvidenceId = Guid.NewGuid(),
            StepExecutionId = Guid.NewGuid(),
            Claim = "E2",
            EvidenceTypeId = (int)Domain.ValueObjects.EvidenceType.Assumption,
            CreatedAt = DateTime.UtcNow
        };

        await _context.Hypotheses.AddRangeAsync(hypothesis1, hypothesis2);
        await _context.Evidences.AddRangeAsync(evidence1, evidence2);
        await _context.SaveChangesAsync();

        await _context.EvidenceHypothesisEvaluations.AddRangeAsync(
            new EvidenceHypothesisEvaluation
            {
                EvidenceHypothesisEvaluationId = Guid.NewGuid(),
                StepExecutionId = Guid.NewGuid(),
                HypothesisId = hypothesis1.HypothesisId,
                EvidenceId = evidence1.EvidenceId,
                EvaluationScoreId = (int)Domain.ValueObjects.EvaluationScore.Consistent,
                CreatedAt = DateTime.UtcNow
            },
            new EvidenceHypothesisEvaluation
            {
                EvidenceHypothesisEvaluationId = Guid.NewGuid(),
                StepExecutionId = Guid.NewGuid(),
                HypothesisId = hypothesis2.HypothesisId,
                EvidenceId = evidence1.EvidenceId,
                EvaluationScoreId = (int)Domain.ValueObjects.EvaluationScore.Inconsistent,
                CreatedAt = DateTime.UtcNow
            },
            new EvidenceHypothesisEvaluation
            {
                EvidenceHypothesisEvaluationId = Guid.NewGuid(),
                StepExecutionId = Guid.NewGuid(),
                HypothesisId = hypothesis1.HypothesisId,
                EvidenceId = evidence2.EvidenceId,
                EvaluationScoreId = (int)Domain.ValueObjects.EvaluationScore.Neutral,
                CreatedAt = DateTime.UtcNow
            }
        );
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByEvidenceIdAsync(evidence1.EvidenceId);

        // Assert
        result.Should().HaveCount(2);
        result.Should().AllSatisfy(e => e.Evidence.Claim.Should().Be("E1"));
    }

    /// <summary>

    /// Verifies that get by evidence id async with no matching evaluations returns empty list.

    /// </summary>

    [Fact]
    public async Task GetByEvidenceIdAsync_WithNoMatchingEvaluations_ReturnsEmptyList()
    {
        // Arrange
        var evidenceId = Guid.NewGuid();

        // Act
        var result = await _repository.GetByEvidenceIdAsync(evidenceId);

        // Assert
        result.Should().BeEmpty();
    }

    /// <summary>

    /// Verifies that Get uses AsNoTracking to prevent EF Core from tracking the entities.

    /// </summary>

    [Fact]
    public async Task GetByEvidenceIdAsync_DoesNotTrackEntities()
    {
        // Arrange
        var hypothesis = new Hypothesis
        {
            HypothesisId = Guid.NewGuid(),
            StepExecutionId = Guid.NewGuid(),
            ShortTitle = "Test",
            HypothesisText = "Test",
            IsRefined = false,
            CreatedAt = DateTime.UtcNow
        };

        var evidence = new Evidence
        {
            EvidenceId = Guid.NewGuid(),
            StepExecutionId = Guid.NewGuid(),
            Claim = "Test",
            EvidenceTypeId = (int)Domain.ValueObjects.EvidenceType.Fact,
            CreatedAt = DateTime.UtcNow
        };

        await _context.Hypotheses.AddAsync(hypothesis);
        await _context.Evidences.AddAsync(evidence);
        await _context.SaveChangesAsync();

        await _context.EvidenceHypothesisEvaluations.AddAsync(
            new EvidenceHypothesisEvaluation
            {
                EvidenceHypothesisEvaluationId = Guid.NewGuid(),
                StepExecutionId = Guid.NewGuid(),
                HypothesisId = hypothesis.HypothesisId,
                EvidenceId = evidence.EvidenceId,
                EvaluationScoreId = (int)Domain.ValueObjects.EvaluationScore.Consistent,
                CreatedAt = DateTime.UtcNow
            }
        );
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByEvidenceIdAsync(evidence.EvidenceId);

        // Assert
        var trackedEntities = _context.ChangeTracker.Entries<EvidenceHypothesisEvaluation>().Count();
        trackedEntities.Should().Be(0);
    }

    #endregion

    #region Integration Tests

    /// <summary>

    /// Verifies the complete workflow of to end save and retrieve works correctly works correctly.

    /// </summary>

    [Fact]
    public async Task EndToEnd_SaveAndRetrieve_WorksCorrectly()
    {
        // Arrange
        var stepExecutionId = Guid.NewGuid();
        var hypothesisId = Guid.NewGuid();
        var evidenceId = Guid.NewGuid();

        var evaluations = new List<DomainEntity.EvidenceHypothesisEvaluation>
        {
            new DomainEntity.EvidenceHypothesisEvaluation
            {
                Hypothesis = new DomainEntity.Hypothesis { ShortTitle = "H1", HypothesisText = "Text1" },
                Evidence = new DomainEntity.Evidence { Claim = "E1", Type = Domain.ValueObjects.EvidenceType.Fact },
                Score = Domain.ValueObjects.EvaluationScore.Consistent,
                ScoreRationale = "Test rationale",
                ConfidenceLevel = 0.8m,
                ConfidenceRationale = "Test confidence"
            }
        };

        var hypothesisIdMap = new Dictionary<string, Guid> { { "H1", hypothesisId } };
        var evidenceIdMap = new Dictionary<string, Guid> { { "E1", evidenceId } };

        // Setup related entities
        await _context.Hypotheses.AddAsync(new Hypothesis
        {
            HypothesisId = hypothesisId,
            StepExecutionId = stepExecutionId,
            ShortTitle = "H1",
            HypothesisText = "Text1",
            IsRefined = false,
            CreatedAt = DateTime.UtcNow
        });

        await _context.Evidences.AddAsync(new Evidence
        {
            EvidenceId = evidenceId,
            StepExecutionId = stepExecutionId,
            Claim = "E1",
            EvidenceTypeId = (int)Domain.ValueObjects.EvidenceType.Fact,
            CreatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        // Act - Save
        await _repository.SaveBatchAsync(evaluations, stepExecutionId, hypothesisIdMap, evidenceIdMap);

        // Act - Retrieve
        var retrieved = await _repository.GetByStepExecutionIdAsync(stepExecutionId);

        // Assert
        retrieved.Should().HaveCount(1);
        retrieved[0].Score.Should().Be(Domain.ValueObjects.EvaluationScore.Consistent);
        retrieved[0].ScoreRationale.Should().Be("Test rationale");
        retrieved[0].ConfidenceLevel.Should().Be(0.8m);
        retrieved[0].Hypothesis.ShortTitle.Should().Be("H1");
        retrieved[0].Evidence.Claim.Should().Be("E1");
    }

    #endregion
}
