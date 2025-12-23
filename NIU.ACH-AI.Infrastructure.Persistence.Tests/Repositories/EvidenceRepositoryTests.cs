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

    [Fact]
    public void Constructor_WithNullContext_ThrowsArgumentNullException()
    {
        // Arrange, Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() => new EvidenceRepository(null!));
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
        var repository = new EvidenceRepository(context);

        // Assert
        repository.Should().NotBeNull();
        
        // Cleanup
        context.Dispose();
    }

    #endregion

    #region SaveBatchAsync Tests

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
                Type = EvidenceType.Fact,
                Notes = "Notes 1"
            },
            new DomainEntity.Evidence
            {
                EvidenceId = Guid.NewGuid(),
                Claim = "Evidence 2",
                ReferenceSnippet = "Snippet 2",
                Type = EvidenceType.Assumption,
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
                Type = EvidenceType.ExpertOpinion,
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
                Type = EvidenceType.Fact
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
                EvidenceTypeId = (int)EvidenceType.Fact,
                CreatedAt = DateTime.UtcNow
            },
            new Evidence
            {
                EvidenceId = Guid.NewGuid(),
                StepExecutionId = stepExecutionId,
                Claim = "Evidence 2",
                EvidenceTypeId = (int)EvidenceType.Assumption,
                CreatedAt = DateTime.UtcNow
            },
            new Evidence
            {
                EvidenceId = Guid.NewGuid(),
                StepExecutionId = otherStepExecutionId,
                Claim = "Other Evidence",
                EvidenceTypeId = (int)EvidenceType.Fact,
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
            EvidenceTypeId = (int)EvidenceType.Fact,
            CreatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByStepExecutionIdAsync(stepExecutionId);

        // Assert
        var trackedEntities = _context.ChangeTracker.Entries<Evidence>().Count();
        trackedEntities.Should().Be(0);
    }

    #endregion

    #region GetByIdAsync Tests

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
            EvidenceTypeId = (int)EvidenceType.Fact,
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
            EvidenceTypeId = (int)EvidenceType.Fact,
            CreatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByIdAsync(evidenceId);

        // Assert
        var trackedEntities = _context.ChangeTracker.Entries<Evidence>().Count();
        trackedEntities.Should().Be(0);
    }

    #endregion

    #region GetByTypeAsync Tests

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
                EvidenceTypeId = (int)EvidenceType.Fact,
                CreatedAt = DateTime.UtcNow
            },
            new Evidence
            {
                EvidenceId = Guid.NewGuid(),
                StepExecutionId = stepExecutionId,
                Claim = "Fact Evidence 2",
                EvidenceTypeId = (int)EvidenceType.Fact,
                CreatedAt = DateTime.UtcNow
            },
            new Evidence
            {
                EvidenceId = Guid.NewGuid(),
                StepExecutionId = stepExecutionId,
                Claim = "Assumption Evidence",
                EvidenceTypeId = (int)EvidenceType.Assumption,
                CreatedAt = DateTime.UtcNow
            }
        );
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByTypeAsync(stepExecutionId, EvidenceType.Fact);

        // Assert
        result.Should().HaveCount(2);
        result.Should().AllSatisfy(e => e.Type.Should().Be(EvidenceType.Fact));
    }

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
            EvidenceTypeId = (int)EvidenceType.Fact,
            CreatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByTypeAsync(stepExecutionId, EvidenceType.ExpertOpinion);

        // Assert
        result.Should().BeEmpty();
    }

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
                EvidenceTypeId = (int)EvidenceType.Fact,
                CreatedAt = DateTime.UtcNow
            },
            new Evidence
            {
                EvidenceId = Guid.NewGuid(),
                StepExecutionId = stepExecutionId2,
                Claim = "Step2 Fact",
                EvidenceTypeId = (int)EvidenceType.Fact,
                CreatedAt = DateTime.UtcNow
            },
            new Evidence
            {
                EvidenceId = Guid.NewGuid(),
                StepExecutionId = stepExecutionId1,
                Claim = "Step1 Assumption",
                EvidenceTypeId = (int)EvidenceType.Assumption,
                CreatedAt = DateTime.UtcNow
            }
        );
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByTypeAsync(stepExecutionId1, EvidenceType.Fact);

        // Assert
        result.Should().HaveCount(1);
        result[0].Claim.Should().Be("Step1 Fact");
    }

    [Theory]
    [InlineData(EvidenceType.Fact)]
    [InlineData(EvidenceType.Assumption)]
    [InlineData(EvidenceType.ExpertOpinion)]
    public async Task GetByTypeAsync_WithDifferentTypes_FiltersCorrectly(EvidenceType type)
    {
        // Arrange
        var stepExecutionId = Guid.NewGuid();
        
        await _context.Evidences.AddRangeAsync(
            new Evidence
            {
                EvidenceId = Guid.NewGuid(),
                StepExecutionId = stepExecutionId,
                Claim = "Fact",
                EvidenceTypeId = (int)EvidenceType.Fact,
                CreatedAt = DateTime.UtcNow
            },
            new Evidence
            {
                EvidenceId = Guid.NewGuid(),
                StepExecutionId = stepExecutionId,
                Claim = "Assumption",
                EvidenceTypeId = (int)EvidenceType.Assumption,
                CreatedAt = DateTime.UtcNow
            },
            new Evidence
            {
                EvidenceId = Guid.NewGuid(),
                StepExecutionId = stepExecutionId,
                Claim = "ExpertOpinion",
                EvidenceTypeId = (int)EvidenceType.ExpertOpinion,
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
            EvidenceTypeId = (int)EvidenceType.Fact,
            CreatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByTypeAsync(stepExecutionId, EvidenceType.Fact);

        // Assert
        var trackedEntities = _context.ChangeTracker.Entries<Evidence>().Count();
        trackedEntities.Should().Be(0);
    }

    #endregion

    #region Integration Tests

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
                Type = EvidenceType.Fact,
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
        retrieved[0].Type.Should().Be(EvidenceType.Fact);
        retrieved[0].Notes.Should().Be("Integration Notes");
    }

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
                Type = EvidenceType.Fact
            }
        };

        var evidenceList2 = new List<DomainEntity.Evidence>
        {
            new DomainEntity.Evidence
            {
                EvidenceId = Guid.NewGuid(),
                Claim = "Evidence Step 2",
                Type = EvidenceType.Assumption
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
