using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NIU.ACH_AI.Domain.ValueObjects;
using NIU.ACH_AI.Infrastructure.Persistence.Models;
using NIU.ACH_AI.Infrastructure.Persistence.Repositories;
using DomainEntity = NIU.ACH_AI.Domain.Entities;

namespace NIU.ACH_AI.Infrastructure.Persistence.Tests.Repositories;

/// <summary>
/// Unit tests for EvidenceRepository following FIRST principles.
/// Uses in-memory database for fast, isolated tests.
/// </summary>
public class EvidenceRepositoryTests : IDisposable
{
    private readonly AchAIDbContext _context;
    private readonly EvidenceRepository _repository;

    public EvidenceRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<AchAIDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new AchAIDbContext(options);
        _repository = new EvidenceRepository(_context);
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
        var exception = Assert.Throws<ArgumentNullException>(() => new EvidenceRepository(null!));
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
        var repository = new EvidenceRepository(context);

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
    public async Task SaveBatchAsync_WithValidEvidenceList_SavesAllEntities()
    {
        // Arrange
        var stepExecutionId = Guid.NewGuid();
        var evidenceList = new List<DomainEntity.Evidence>
        {
            new DomainEntity.Evidence
            {
                EvidenceId = Guid.NewGuid(),
                Claim = "Evidence 1",
                ReferenceSnippet = "Snippet 1",
                Type = Domain.ValueObjects.EvidenceType.Fact,
                Notes = "Notes 1"
            },
            new DomainEntity.Evidence
            {
                EvidenceId = Guid.NewGuid(),
                Claim = "Evidence 2",
                ReferenceSnippet = "Snippet 2",
                Type = Domain.ValueObjects.EvidenceType.Assumption,
                Notes = "Notes 2"
            }
        };

        // Act
        await _repository.SaveBatchAsync(evidenceList, stepExecutionId);

        // Assert
        var savedEvidence = await _context.Evidences.ToListAsync();
        savedEvidence.Should().HaveCount(2);
        savedEvidence[0].Claim.Should().Be("Evidence 1");
        savedEvidence[1].Claim.Should().Be("Evidence 2");
        savedEvidence.All(e => e.StepExecutionId == stepExecutionId).Should().BeTrue();
    }

    /// <summary>

    /// Verifies that Save handles null input gracefully without throwing exceptions.

    /// </summary>

    [Fact]
    public async Task SaveBatchAsync_WithNullEvidenceList_DoesNothing()
    {
        // Arrange
        var stepExecutionId = Guid.NewGuid();

        // Act
        await _repository.SaveBatchAsync(null!, stepExecutionId);

        // Assert
        var savedEvidence = await _context.Evidences.ToListAsync();
        savedEvidence.Should().BeEmpty();
    }

    /// <summary>

    /// Verifies that Save handles empty collections gracefully without saving anything.

    /// </summary>

    [Fact]
    public async Task SaveBatchAsync_WithEmptyEvidenceList_DoesNothing()
    {
        // Arrange
        var stepExecutionId = Guid.NewGuid();
        var evidenceList = new List<DomainEntity.Evidence>();

        // Act
        await _repository.SaveBatchAsync(evidenceList, stepExecutionId);

        // Assert
        var savedEvidence = await _context.Evidences.ToListAsync();
        savedEvidence.Should().BeEmpty();
    }

    /// <summary>

    /// Verifies that save batch async with single evidence saves successfully.

    /// </summary>

    [Fact]
    public async Task SaveBatchAsync_WithSingleEvidence_SavesSuccessfully()
    {
        // Arrange
        var stepExecutionId = Guid.NewGuid();
        var evidenceList = new List<DomainEntity.Evidence>
        {
            new DomainEntity.Evidence
            {
                EvidenceId = Guid.NewGuid(),
                Claim = "Single Evidence",
                ReferenceSnippet = "Single Snippet",
                Type = Domain.ValueObjects.EvidenceType.ExpertOpinion,
                Notes = "Single Notes"
            }
        };

        // Act
        await _repository.SaveBatchAsync(evidenceList, stepExecutionId);

        // Assert
        var savedEvidence = await _context.Evidences.ToListAsync();
        savedEvidence.Should().HaveCount(1);
        savedEvidence[0].Claim.Should().Be("Single Evidence");
    }

    /// <summary>

    /// Verifies that save batch async with cancellation token respects token.

    /// </summary>

    [Fact]
    public async Task SaveBatchAsync_WithCancellationToken_RespectsToken()
    {
        // Arrange
        var stepExecutionId = Guid.NewGuid();
        var evidenceList = new List<DomainEntity.Evidence>
        {
            new DomainEntity.Evidence
            {
                EvidenceId = Guid.NewGuid(),
                Claim = "Test Evidence",
                Type = Domain.ValueObjects.EvidenceType.Fact
            }
        };
        var cancellationToken = new CancellationToken();

        // Act
        await _repository.SaveBatchAsync(evidenceList, stepExecutionId, cancellationToken);

        // Assert
        var savedEvidence = await _context.Evidences.ToListAsync();
        savedEvidence.Should().HaveCount(1);
    }

    #endregion

    #region GetByStepExecutionIdAsync Tests

    /// <summary>

    /// Verifies that Get returns the correct by step when it exists in the database.

    /// </summary>

    [Fact]
    public async Task GetByStepExecutionIdAsync_WithExistingEvidence_ReturnsAllMatchingEvidence()
    {
        // Arrange
        var stepExecutionId = Guid.NewGuid();
        var otherStepExecutionId = Guid.NewGuid();
        
        await _context.Evidences.AddRangeAsync(
            new Evidence
            {
                EvidenceId = Guid.NewGuid(),
                StepExecutionId = stepExecutionId,
                Claim = "Evidence 1",
                EvidenceTypeId = (int)Domain.ValueObjects.EvidenceType.Fact,
                CreatedAt = DateTime.UtcNow
            },
            new Evidence
            {
                EvidenceId = Guid.NewGuid(),
                StepExecutionId = stepExecutionId,
                Claim = "Evidence 2",
                EvidenceTypeId = (int)Domain.ValueObjects.EvidenceType.Assumption,
                CreatedAt = DateTime.UtcNow
            },
            new Evidence
            {
                EvidenceId = Guid.NewGuid(),
                StepExecutionId = otherStepExecutionId,
                Claim = "Other Evidence",
                EvidenceTypeId = (int)Domain.ValueObjects.EvidenceType.Fact,
                CreatedAt = DateTime.UtcNow
            }
        );
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByStepExecutionIdAsync(stepExecutionId);

        // Assert
        result.Should().HaveCount(2);
        result.Should().AllSatisfy(e => e.Claim.Should().Match(c => c.StartsWith("Evidence")));
    }

    /// <summary>

    /// Verifies that get by step execution id async with no matching evidence returns empty list.

    /// </summary>

    [Fact]
    public async Task GetByStepExecutionIdAsync_WithNoMatchingEvidence_ReturnsEmptyList()
    {
        // Arrange
        var stepExecutionId = Guid.NewGuid();

        // Act
        var result = await _repository.GetByStepExecutionIdAsync(stepExecutionId);

        // Assert
        result.Should().BeEmpty();
    }

    /// <summary>

    /// Verifies that Get uses AsNoTracking to prevent EF Core from tracking the entities.

    /// </summary>

    [Fact]
    public async Task GetByStepExecutionIdAsync_DoesNotTrackEntities()
    {
        // Arrange
        var stepExecutionId = Guid.NewGuid();
        await _context.Evidences.AddAsync(new Evidence
        {
            EvidenceId = Guid.NewGuid(),
            StepExecutionId = stepExecutionId,
            Claim = "Test Evidence",
            EvidenceTypeId = (int)Domain.ValueObjects.EvidenceType.Fact,
            CreatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        // Act
        var result = await _repository.GetByStepExecutionIdAsync(stepExecutionId);

        // Assert
        var trackedEntities = _context.ChangeTracker.Entries<Evidence>().Count();
        trackedEntities.Should().Be(0);
    }

    #endregion

    #region GetByIdAsync Tests

    /// <summary>

    /// Verifies that Get returns the correct by id when it exists in the database.

    /// </summary>

    [Fact]
    public async Task GetByIdAsync_WithExistingId_ReturnsEvidence()
    {
        // Arrange
        var evidenceId = Guid.NewGuid();
        var stepExecutionId = Guid.NewGuid();
        
        await _context.Evidences.AddAsync(new Evidence
        {
            EvidenceId = evidenceId,
            StepExecutionId = stepExecutionId,
            Claim = "Test Evidence",
            ReferenceSnippet = "Test Snippet",
            EvidenceTypeId = (int)Domain.ValueObjects.EvidenceType.Fact,
            Notes = "Test Notes",
            CreatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByIdAsync(evidenceId);

        // Assert
        result.Should().NotBeNull();
        result!.EvidenceId.Should().Be(evidenceId);
        result.Claim.Should().Be("Test Evidence");
    }

    /// <summary>

    /// Verifies that Get returns null when the by id does not exist.

    /// </summary>

    [Fact]
    public async Task GetByIdAsync_WithNonExistingId_ReturnsNull()
    {
        // Arrange
        var evidenceId = Guid.NewGuid();

        // Act
        var result = await _repository.GetByIdAsync(evidenceId);

        // Assert
        result.Should().BeNull();
    }

    /// <summary>

    /// Verifies that Get uses AsNoTracking to prevent EF Core from tracking the entities.

    /// </summary>

    [Fact]
    public async Task GetByIdAsync_DoesNotTrackEntity()
    {
        // Arrange
        var evidenceId = Guid.NewGuid();
        await _context.Evidences.AddAsync(new Evidence
        {
            EvidenceId = evidenceId,
            StepExecutionId = Guid.NewGuid(),
            Claim = "Test Evidence",
            EvidenceTypeId = (int)Domain.ValueObjects.EvidenceType.Fact,
            CreatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        // Act
        var result = await _repository.GetByIdAsync(evidenceId);

        // Assert
        var trackedEntities = _context.ChangeTracker.Entries<Evidence>().Count();
        trackedEntities.Should().Be(0);
    }

    #endregion

    #region GetByTypeAsync Tests

    /// <summary>

    /// Verifies that Get returns the correct by type when it exists in the database.

    /// </summary>

    [Fact]
    public async Task GetByTypeAsync_WithExistingType_ReturnsMatchingEvidence()
    {
        // Arrange
        var stepExecutionId = Guid.NewGuid();
        
        await _context.Evidences.AddRangeAsync(
            new Evidence
            {
                EvidenceId = Guid.NewGuid(),
                StepExecutionId = stepExecutionId,
                Claim = "Fact Evidence 1",
                EvidenceTypeId = (int)Domain.ValueObjects.EvidenceType.Fact,
                CreatedAt = DateTime.UtcNow
            },
            new Evidence
            {
                EvidenceId = Guid.NewGuid(),
                StepExecutionId = stepExecutionId,
                Claim = "Fact Evidence 2",
                EvidenceTypeId = (int)Domain.ValueObjects.EvidenceType.Fact,
                CreatedAt = DateTime.UtcNow
            },
            new Evidence
            {
                EvidenceId = Guid.NewGuid(),
                StepExecutionId = stepExecutionId,
                Claim = "Assumption Evidence",
                EvidenceTypeId = (int)Domain.ValueObjects.EvidenceType.Assumption,
                CreatedAt = DateTime.UtcNow
            }
        );
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByTypeAsync(stepExecutionId, Domain.ValueObjects.EvidenceType.Fact);

        // Assert
        result.Should().HaveCount(2);
        result.Should().AllSatisfy(e => e.Type.Should().Be(Domain.ValueObjects.EvidenceType.Fact));
    }

    /// <summary>

    /// Verifies that get by type async with no matching type returns empty list.

    /// </summary>

    [Fact]
    public async Task GetByTypeAsync_WithNoMatchingType_ReturnsEmptyList()
    {
        // Arrange
        var stepExecutionId = Guid.NewGuid();
        
        await _context.Evidences.AddAsync(new Evidence
        {
            EvidenceId = Guid.NewGuid(),
            StepExecutionId = stepExecutionId,
            Claim = "Fact Evidence",
            EvidenceTypeId = (int)Domain.ValueObjects.EvidenceType.Fact,
            CreatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByTypeAsync(stepExecutionId, Domain.ValueObjects.EvidenceType.ExpertOpinion);

        // Assert
        result.Should().BeEmpty();
    }

    /// <summary>

    /// Verifies that Get correctly filters results based on type async filtersby step execution id and type.

    /// </summary>

    [Fact]
    public async Task GetByTypeAsync_FiltersbyStepExecutionIdAndType()
    {
        // Arrange
        var stepExecutionId1 = Guid.NewGuid();
        var stepExecutionId2 = Guid.NewGuid();
        
        await _context.Evidences.AddRangeAsync(
            new Evidence
            {
                EvidenceId = Guid.NewGuid(),
                StepExecutionId = stepExecutionId1,
                Claim = "Step1 Fact",
                EvidenceTypeId = (int)Domain.ValueObjects.EvidenceType.Fact,
                CreatedAt = DateTime.UtcNow
            },
            new Evidence
            {
                EvidenceId = Guid.NewGuid(),
                StepExecutionId = stepExecutionId2,
                Claim = "Step2 Fact",
                EvidenceTypeId = (int)Domain.ValueObjects.EvidenceType.Fact,
                CreatedAt = DateTime.UtcNow
            },
            new Evidence
            {
                EvidenceId = Guid.NewGuid(),
                StepExecutionId = stepExecutionId1,
                Claim = "Step1 Assumption",
                EvidenceTypeId = (int)Domain.ValueObjects.EvidenceType.Assumption,
                CreatedAt = DateTime.UtcNow
            }
        );
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByTypeAsync(stepExecutionId1, Domain.ValueObjects.EvidenceType.Fact);

        // Assert
        result.Should().HaveCount(1);
        result[0].Claim.Should().Be("Step1 Fact");
    }

    [Theory]
    [InlineData(Domain.ValueObjects.EvidenceType.Fact)]
    [InlineData(Domain.ValueObjects.EvidenceType.Assumption)]
    [InlineData(Domain.ValueObjects.EvidenceType.ExpertOpinion)]
    public async Task GetByTypeAsync_WithDifferentTypes_FiltersCorrectly(Domain.ValueObjects.EvidenceType type)
    {
        // Arrange
        var stepExecutionId = Guid.NewGuid();
        
        await _context.Evidences.AddRangeAsync(
            new Evidence
            {
                EvidenceId = Guid.NewGuid(),
                StepExecutionId = stepExecutionId,
                Claim = "Fact",
                EvidenceTypeId = (int)Domain.ValueObjects.EvidenceType.Fact,
                CreatedAt = DateTime.UtcNow
            },
            new Evidence
            {
                EvidenceId = Guid.NewGuid(),
                StepExecutionId = stepExecutionId,
                Claim = "Assumption",
                EvidenceTypeId = (int)Domain.ValueObjects.EvidenceType.Assumption,
                CreatedAt = DateTime.UtcNow
            },
            new Evidence
            {
                EvidenceId = Guid.NewGuid(),
                StepExecutionId = stepExecutionId,
                Claim = "ExpertOpinion",
                EvidenceTypeId = (int)Domain.ValueObjects.EvidenceType.ExpertOpinion,
                CreatedAt = DateTime.UtcNow
            }
        );
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByTypeAsync(stepExecutionId, type);

        // Assert
        result.Should().HaveCount(1);
        result[0].Type.Should().Be(type);
    }

    /// <summary>

    /// Verifies that Get uses AsNoTracking to prevent EF Core from tracking the entities.

    /// </summary>

    [Fact]
    public async Task GetByTypeAsync_DoesNotTrackEntities()
    {
        // Arrange
        var stepExecutionId = Guid.NewGuid();
        await _context.Evidences.AddAsync(new Evidence
        {
            EvidenceId = Guid.NewGuid(),
            StepExecutionId = stepExecutionId,
            Claim = "Test Evidence",
            EvidenceTypeId = (int)Domain.ValueObjects.EvidenceType.Fact,
            CreatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        // Act
        var result = await _repository.GetByTypeAsync(stepExecutionId, Domain.ValueObjects.EvidenceType.Fact);

        // Assert
        var trackedEntities = _context.ChangeTracker.Entries<Evidence>().Count();
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
        var evidenceList = new List<DomainEntity.Evidence>
        {
            new DomainEntity.Evidence
            {
                EvidenceId = Guid.NewGuid(),
                Claim = "Integration Test Evidence",
                ReferenceSnippet = "Integration Snippet",
                Type = Domain.ValueObjects.EvidenceType.Fact,
                Notes = "Integration Notes"
            }
        };

        // Act - Save
        await _repository.SaveBatchAsync(evidenceList, stepExecutionId);

        // Act - Retrieve
        var retrieved = await _repository.GetByStepExecutionIdAsync(stepExecutionId);

        // Assert
        retrieved.Should().HaveCount(1);
        retrieved[0].Claim.Should().Be("Integration Test Evidence");
        retrieved[0].ReferenceSnippet.Should().Be("Integration Snippet");
        retrieved[0].Type.Should().Be(Domain.ValueObjects.EvidenceType.Fact);
        retrieved[0].Notes.Should().Be("Integration Notes");
    }

    /// <summary>

    /// Verifies that multiple operations on same context work independently.

    /// </summary>

    [Fact]
    public async Task MultipleOperations_OnSameContext_WorkIndependently()
    {
        // Arrange
        var stepExecutionId1 = Guid.NewGuid();
        var stepExecutionId2 = Guid.NewGuid();

        var evidenceList1 = new List<DomainEntity.Evidence>
        {
            new DomainEntity.Evidence
            {
                EvidenceId = Guid.NewGuid(),
                Claim = "Evidence Step 1",
                Type = Domain.ValueObjects.EvidenceType.Fact
            }
        };

        var evidenceList2 = new List<DomainEntity.Evidence>
        {
            new DomainEntity.Evidence
            {
                EvidenceId = Guid.NewGuid(),
                Claim = "Evidence Step 2",
                Type = Domain.ValueObjects.EvidenceType.Assumption
            }
        };

        // Act
        await _repository.SaveBatchAsync(evidenceList1, stepExecutionId1);
        await _repository.SaveBatchAsync(evidenceList2, stepExecutionId2);

        var result1 = await _repository.GetByStepExecutionIdAsync(stepExecutionId1);
        var result2 = await _repository.GetByStepExecutionIdAsync(stepExecutionId2);

        // Assert
        result1.Should().HaveCount(1);
        result1[0].Claim.Should().Be("Evidence Step 1");
        
        result2.Should().HaveCount(1);
        result2[0].Claim.Should().Be("Evidence Step 2");
    }

    #endregion
}
