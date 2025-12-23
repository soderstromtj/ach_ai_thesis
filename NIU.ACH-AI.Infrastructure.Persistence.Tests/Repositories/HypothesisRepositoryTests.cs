using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NIU.ACH_AI.Infrastructure.Persistence.Models;
using NIU.ACH_AI.Infrastructure.Persistence.Repositories;
using DomainEntity = NIU.ACH_AI.Domain.Entities;

namespace NIU.ACH_AI.Infrastructure.Persistence.Tests.Repositories;

/// <summary>
/// Unit tests for HypothesisRepository following FIRST principles.
/// Uses in-memory database for fast, isolated tests.
/// </summary>
public class HypothesisRepositoryTests : IDisposable
{
    private readonly AchAIDbContext _context;
    private readonly HypothesisRepository _repository;

    public HypothesisRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<AchAIDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new AchAIDbContext(options);
        _repository = new HypothesisRepository(_context);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullContext_ThrowsArgumentNullException()
    {
        // Arrange, Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() => new HypothesisRepository(null!));
        exception.ParamName.Should().Be("context");
    }

    [Fact]
    public void Constructor_WithValidContext_CreatesInstance()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<AchAIDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        var context = new AchAIDbContext(options);

        // Act
        var repository = new HypothesisRepository(context);

        // Assert
        repository.Should().NotBeNull();
        
        // Cleanup
        context.Dispose();
    }

    #endregion

    #region SaveBatchAsync Tests

    [Fact]
    public async Task SaveBatchAsync_WithValidHypothesisList_SavesAllEntities()
    {
        // Arrange
        var stepExecutionId = Guid.NewGuid();
        var hypotheses = new List<DomainEntity.Hypothesis>
        {
            new DomainEntity.Hypothesis
            {
                ShortTitle = "Hypothesis 1",
                HypothesisText = "Text 1"
            },
            new DomainEntity.Hypothesis
            {
                ShortTitle = "Hypothesis 2",
                HypothesisText = "Text 2"
            }
        };

        // Act
        await _repository.SaveBatchAsync(hypotheses, stepExecutionId);

        // Assert
        var savedHypotheses = await _context.Hypotheses.ToListAsync();
        savedHypotheses.Should().HaveCount(2);
        savedHypotheses[0].ShortTitle.Should().Be("Hypothesis 1");
        savedHypotheses[1].ShortTitle.Should().Be("Hypothesis 2");
        savedHypotheses.All(h => h.StepExecutionId == stepExecutionId).Should().BeTrue();
        savedHypotheses.All(h => h.IsRefined == false).Should().BeTrue();
    }

    [Fact]
    public async Task SaveBatchAsync_WithNullHypothesisList_DoesNothing()
    {
        // Arrange
        var stepExecutionId = Guid.NewGuid();

        // Act
        await _repository.SaveBatchAsync(null!, stepExecutionId);

        // Assert
        var savedHypotheses = await _context.Hypotheses.ToListAsync();
        savedHypotheses.Should().BeEmpty();
    }

    [Fact]
    public async Task SaveBatchAsync_WithEmptyHypothesisList_DoesNothing()
    {
        // Arrange
        var stepExecutionId = Guid.NewGuid();
        var hypotheses = new List<DomainEntity.Hypothesis>();

        // Act
        await _repository.SaveBatchAsync(hypotheses, stepExecutionId);

        // Assert
        var savedHypotheses = await _context.Hypotheses.ToListAsync();
        savedHypotheses.Should().BeEmpty();
    }

    [Fact]
    public async Task SaveBatchAsync_WithSingleHypothesis_SavesSuccessfully()
    {
        // Arrange
        var stepExecutionId = Guid.NewGuid();
        var hypotheses = new List<DomainEntity.Hypothesis>
        {
            new DomainEntity.Hypothesis
            {
                ShortTitle = "Single Hypothesis",
                HypothesisText = "Single text"
            }
        };

        // Act
        await _repository.SaveBatchAsync(hypotheses, stepExecutionId);

        // Assert
        var savedHypotheses = await _context.Hypotheses.ToListAsync();
        savedHypotheses.Should().HaveCount(1);
        savedHypotheses[0].ShortTitle.Should().Be("Single Hypothesis");
    }

    [Fact]
    public async Task SaveBatchAsync_GeneratesUniqueGuidsForEachHypothesis()
    {
        // Arrange
        var stepExecutionId = Guid.NewGuid();
        var hypotheses = new List<DomainEntity.Hypothesis>
        {
            new DomainEntity.Hypothesis { ShortTitle = "H1", HypothesisText = "T1" },
            new DomainEntity.Hypothesis { ShortTitle = "H2", HypothesisText = "T2" },
            new DomainEntity.Hypothesis { ShortTitle = "H3", HypothesisText = "T3" }
        };

        // Act
        await _repository.SaveBatchAsync(hypotheses, stepExecutionId);

        // Assert
        var savedHypotheses = await _context.Hypotheses.ToListAsync();
        var ids = savedHypotheses.Select(h => h.HypothesisId).ToList();
        ids.Should().OnlyHaveUniqueItems();
        ids.Should().AllSatisfy(id => id.Should().NotBe(Guid.Empty));
    }

    [Fact]
    public async Task SaveBatchAsync_WithCancellationToken_RespectsToken()
    {
        // Arrange
        var stepExecutionId = Guid.NewGuid();
        var hypotheses = new List<DomainEntity.Hypothesis>
        {
            new DomainEntity.Hypothesis { ShortTitle = "Test", HypothesisText = "Test" }
        };
        var cancellationToken = new CancellationToken();

        // Act
        await _repository.SaveBatchAsync(hypotheses, stepExecutionId, cancellationToken);

        // Assert
        var savedHypotheses = await _context.Hypotheses.ToListAsync();
        savedHypotheses.Should().HaveCount(1);
    }

    #endregion

    #region GetByStepExecutionIdAsync Tests

    [Fact]
    public async Task GetByStepExecutionIdAsync_WithExistingHypotheses_ReturnsAllMatching()
    {
        // Arrange
        var stepExecutionId = Guid.NewGuid();
        var otherStepExecutionId = Guid.NewGuid();
        
        await _context.Hypotheses.AddRangeAsync(
            new Hypothesis
            {
                HypothesisId = Guid.NewGuid(),
                StepExecutionId = stepExecutionId,
                ShortTitle = "Hypothesis 1",
                HypothesisText = "Text 1",
                IsRefined = false,
                CreatedAt = DateTime.UtcNow
            },
            new Hypothesis
            {
                HypothesisId = Guid.NewGuid(),
                StepExecutionId = stepExecutionId,
                ShortTitle = "Hypothesis 2",
                HypothesisText = "Text 2",
                IsRefined = false,
                CreatedAt = DateTime.UtcNow
            },
            new Hypothesis
            {
                HypothesisId = Guid.NewGuid(),
                StepExecutionId = otherStepExecutionId,
                ShortTitle = "Other Hypothesis",
                HypothesisText = "Other Text",
                IsRefined = false,
                CreatedAt = DateTime.UtcNow
            }
        );
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByStepExecutionIdAsync(stepExecutionId);

        // Assert
        result.Should().HaveCount(2);
        result.Should().AllSatisfy(h => h.ShortTitle.Should().Match(t => t.StartsWith("Hypothesis")));
    }

    [Fact]
    public async Task GetByStepExecutionIdAsync_WithNoMatchingHypotheses_ReturnsEmptyList()
    {
        // Arrange
        var stepExecutionId = Guid.NewGuid();

        // Act
        var result = await _repository.GetByStepExecutionIdAsync(stepExecutionId);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetByStepExecutionIdAsync_ReturnsRefinedAndUnrefinedHypotheses()
    {
        // Arrange
        var stepExecutionId = Guid.NewGuid();
        
        await _context.Hypotheses.AddRangeAsync(
            new Hypothesis
            {
                HypothesisId = Guid.NewGuid(),
                StepExecutionId = stepExecutionId,
                ShortTitle = "Unrefined",
                HypothesisText = "Text",
                IsRefined = false,
                CreatedAt = DateTime.UtcNow
            },
            new Hypothesis
            {
                HypothesisId = Guid.NewGuid(),
                StepExecutionId = stepExecutionId,
                ShortTitle = "Refined",
                HypothesisText = "Text",
                IsRefined = true,
                CreatedAt = DateTime.UtcNow
            }
        );
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByStepExecutionIdAsync(stepExecutionId);

        // Assert
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetByStepExecutionIdAsync_DoesNotTrackEntities()
    {
        // Arrange
        var stepExecutionId = Guid.NewGuid();
        await _context.Hypotheses.AddAsync(new Hypothesis
        {
            HypothesisId = Guid.NewGuid(),
            StepExecutionId = stepExecutionId,
            ShortTitle = "Test",
            HypothesisText = "Test",
            IsRefined = false,
            CreatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByStepExecutionIdAsync(stepExecutionId);

        // Assert
        var trackedEntities = _context.ChangeTracker.Entries<Hypothesis>().Count();
        trackedEntities.Should().Be(0);
    }

    #endregion

    #region GetByIdAsync Tests

    [Fact]
    public async Task GetByIdAsync_WithExistingId_ReturnsHypothesis()
    {
        // Arrange
        var hypothesisId = Guid.NewGuid();
        var stepExecutionId = Guid.NewGuid();
        
        await _context.Hypotheses.AddAsync(new Hypothesis
        {
            HypothesisId = hypothesisId,
            StepExecutionId = stepExecutionId,
            ShortTitle = "Test Hypothesis",
            HypothesisText = "Test Text",
            IsRefined = false,
            CreatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByIdAsync(hypothesisId);

        // Assert
        result.Should().NotBeNull();
        result!.ShortTitle.Should().Be("Test Hypothesis");
        result.HypothesisText.Should().Be("Test Text");
    }

    [Fact]
    public async Task GetByIdAsync_WithNonExistingId_ReturnsNull()
    {
        // Arrange
        var hypothesisId = Guid.NewGuid();

        // Act
        var result = await _repository.GetByIdAsync(hypothesisId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByIdAsync_DoesNotTrackEntity()
    {
        // Arrange
        var hypothesisId = Guid.NewGuid();
        await _context.Hypotheses.AddAsync(new Hypothesis
        {
            HypothesisId = hypothesisId,
            StepExecutionId = Guid.NewGuid(),
            ShortTitle = "Test",
            HypothesisText = "Test",
            IsRefined = false,
            CreatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByIdAsync(hypothesisId);

        // Assert
        var trackedEntities = _context.ChangeTracker.Entries<Hypothesis>().Count();
        trackedEntities.Should().Be(0);
    }

    #endregion

    #region GetRefinedByStepExecutionIdAsync Tests

    [Fact]
    public async Task GetRefinedByStepExecutionIdAsync_WithRefinedHypotheses_ReturnsOnlyRefined()
    {
        // Arrange
        var stepExecutionId = Guid.NewGuid();
        
        await _context.Hypotheses.AddRangeAsync(
            new Hypothesis
            {
                HypothesisId = Guid.NewGuid(),
                StepExecutionId = stepExecutionId,
                ShortTitle = "Refined 1",
                HypothesisText = "Text 1",
                IsRefined = true,
                CreatedAt = DateTime.UtcNow
            },
            new Hypothesis
            {
                HypothesisId = Guid.NewGuid(),
                StepExecutionId = stepExecutionId,
                ShortTitle = "Refined 2",
                HypothesisText = "Text 2",
                IsRefined = true,
                CreatedAt = DateTime.UtcNow
            },
            new Hypothesis
            {
                HypothesisId = Guid.NewGuid(),
                StepExecutionId = stepExecutionId,
                ShortTitle = "Unrefined",
                HypothesisText = "Text 3",
                IsRefined = false,
                CreatedAt = DateTime.UtcNow
            }
        );
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetRefinedByStepExecutionIdAsync(stepExecutionId);

        // Assert
        result.Should().HaveCount(2);
        result.Should().AllSatisfy(h => h.ShortTitle.Should().StartWith("Refined"));
    }

    [Fact]
    public async Task GetRefinedByStepExecutionIdAsync_WithNoRefinedHypotheses_ReturnsEmptyList()
    {
        // Arrange
        var stepExecutionId = Guid.NewGuid();
        
        await _context.Hypotheses.AddAsync(new Hypothesis
        {
            HypothesisId = Guid.NewGuid(),
            StepExecutionId = stepExecutionId,
            ShortTitle = "Unrefined",
            HypothesisText = "Text",
            IsRefined = false,
            CreatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetRefinedByStepExecutionIdAsync(stepExecutionId);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetRefinedByStepExecutionIdAsync_FiltersbyStepExecutionId()
    {
        // Arrange
        var stepExecutionId1 = Guid.NewGuid();
        var stepExecutionId2 = Guid.NewGuid();
        
        await _context.Hypotheses.AddRangeAsync(
            new Hypothesis
            {
                HypothesisId = Guid.NewGuid(),
                StepExecutionId = stepExecutionId1,
                ShortTitle = "Refined Step 1",
                HypothesisText = "Text",
                IsRefined = true,
                CreatedAt = DateTime.UtcNow
            },
            new Hypothesis
            {
                HypothesisId = Guid.NewGuid(),
                StepExecutionId = stepExecutionId2,
                ShortTitle = "Refined Step 2",
                HypothesisText = "Text",
                IsRefined = true,
                CreatedAt = DateTime.UtcNow
            }
        );
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetRefinedByStepExecutionIdAsync(stepExecutionId1);

        // Assert
        result.Should().HaveCount(1);
        result[0].ShortTitle.Should().Be("Refined Step 1");
    }

    [Fact]
    public async Task GetRefinedByStepExecutionIdAsync_DoesNotTrackEntities()
    {
        // Arrange
        var stepExecutionId = Guid.NewGuid();
        await _context.Hypotheses.AddAsync(new Hypothesis
        {
            HypothesisId = Guid.NewGuid(),
            StepExecutionId = stepExecutionId,
            ShortTitle = "Refined",
            HypothesisText = "Text",
            IsRefined = true,
            CreatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetRefinedByStepExecutionIdAsync(stepExecutionId);

        // Assert
        var trackedEntities = _context.ChangeTracker.Entries<Hypothesis>().Count();
        trackedEntities.Should().Be(0);
    }

    #endregion

    #region MarkAsRefinedAsync Tests

    [Fact]
    public async Task MarkAsRefinedAsync_WithExistingHypothesis_SetsIsRefinedToTrue()
    {
        // Arrange
        var hypothesisId = Guid.NewGuid();
        await _context.Hypotheses.AddAsync(new Hypothesis
        {
            HypothesisId = hypothesisId,
            StepExecutionId = Guid.NewGuid(),
            ShortTitle = "Test",
            HypothesisText = "Test",
            IsRefined = false,
            CreatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        // Act
        await _repository.MarkAsRefinedAsync(hypothesisId);

        // Assert
        var hypothesis = await _context.Hypotheses.FindAsync(hypothesisId);
        hypothesis!.IsRefined.Should().BeTrue();
    }

    [Fact]
    public async Task MarkAsRefinedAsync_WithNonExistingHypothesis_DoesNotThrow()
    {
        // Arrange
        var hypothesisId = Guid.NewGuid();

        // Act & Assert
        await _repository.Invoking(r => r.MarkAsRefinedAsync(hypothesisId))
            .Should().NotThrowAsync();
    }

    [Fact]
    public async Task MarkAsRefinedAsync_WithAlreadyRefinedHypothesis_RemainsRefined()
    {
        // Arrange
        var hypothesisId = Guid.NewGuid();
        await _context.Hypotheses.AddAsync(new Hypothesis
        {
            HypothesisId = hypothesisId,
            StepExecutionId = Guid.NewGuid(),
            ShortTitle = "Test",
            HypothesisText = "Test",
            IsRefined = true,
            CreatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        // Act
        await _repository.MarkAsRefinedAsync(hypothesisId);

        // Assert
        var hypothesis = await _context.Hypotheses.FindAsync(hypothesisId);
        hypothesis!.IsRefined.Should().BeTrue();
    }

    [Fact]
    public async Task MarkAsRefinedAsync_WithCancellationToken_RespectsToken()
    {
        // Arrange
        var hypothesisId = Guid.NewGuid();
        await _context.Hypotheses.AddAsync(new Hypothesis
        {
            HypothesisId = hypothesisId,
            StepExecutionId = Guid.NewGuid(),
            ShortTitle = "Test",
            HypothesisText = "Test",
            IsRefined = false,
            CreatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();
        var cancellationToken = new CancellationToken();

        // Act
        await _repository.MarkAsRefinedAsync(hypothesisId, cancellationToken);

        // Assert
        var hypothesis = await _context.Hypotheses.FindAsync(hypothesisId);
        hypothesis!.IsRefined.Should().BeTrue();
    }

    #endregion

    #region Integration Tests

    [Fact]
    public async Task EndToEnd_SaveAndRetrieve_WorksCorrectly()
    {
        // Arrange
        var stepExecutionId = Guid.NewGuid();
        var hypotheses = new List<DomainEntity.Hypothesis>
        {
            new DomainEntity.Hypothesis
            {
                ShortTitle = "Integration Test",
                HypothesisText = "Integration test hypothesis"
            }
        };

        // Act - Save
        await _repository.SaveBatchAsync(hypotheses, stepExecutionId);

        // Act - Retrieve
        var retrieved = await _repository.GetByStepExecutionIdAsync(stepExecutionId);

        // Assert
        retrieved.Should().HaveCount(1);
        retrieved[0].ShortTitle.Should().Be("Integration Test");
        retrieved[0].HypothesisText.Should().Be("Integration test hypothesis");
    }

    [Fact]
    public async Task EndToEnd_SaveRetrieveAndMarkRefined_WorksCorrectly()
    {
        // Arrange
        var stepExecutionId = Guid.NewGuid();
        var hypotheses = new List<DomainEntity.Hypothesis>
        {
            new DomainEntity.Hypothesis
            {
                ShortTitle = "Test",
                HypothesisText = "Test hypothesis"
            }
        };

        // Act - Save
        await _repository.SaveBatchAsync(hypotheses, stepExecutionId);

        // Act - Get ID
        var allHypotheses = await _context.Hypotheses
            .Where(h => h.StepExecutionId == stepExecutionId)
            .ToListAsync();
        var hypothesisId = allHypotheses[0].HypothesisId;

        // Act - Mark as refined
        await _repository.MarkAsRefinedAsync(hypothesisId);

        // Act - Retrieve refined
        var refined = await _repository.GetRefinedByStepExecutionIdAsync(stepExecutionId);

        // Assert
        refined.Should().HaveCount(1);
        refined[0].ShortTitle.Should().Be("Test");
    }

    [Fact]
    public async Task MultipleOperations_OnSameContext_WorkIndependently()
    {
        // Arrange
        var stepExecutionId1 = Guid.NewGuid();
        var stepExecutionId2 = Guid.NewGuid();

        var hypotheses1 = new List<DomainEntity.Hypothesis>
        {
            new DomainEntity.Hypothesis { ShortTitle = "H1", HypothesisText = "T1" }
        };

        var hypotheses2 = new List<DomainEntity.Hypothesis>
        {
            new DomainEntity.Hypothesis { ShortTitle = "H2", HypothesisText = "T2" }
        };

        // Act
        await _repository.SaveBatchAsync(hypotheses1, stepExecutionId1);
        await _repository.SaveBatchAsync(hypotheses2, stepExecutionId2);

        var result1 = await _repository.GetByStepExecutionIdAsync(stepExecutionId1);
        var result2 = await _repository.GetByStepExecutionIdAsync(stepExecutionId2);

        // Assert
        result1.Should().HaveCount(1);
        result1[0].ShortTitle.Should().Be("H1");
        
        result2.Should().HaveCount(1);
        result2[0].ShortTitle.Should().Be("H2");
    }

    #endregion
}
