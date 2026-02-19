using FluentAssertions;
using NIU.ACH_AI.Domain.ValueObjects;
using NIU.ACH_AI.Infrastructure.Persistence.Mappers;
using DomainEntity = NIU.ACH_AI.Domain.Entities;
using DbModel = NIU.ACH_AI.Infrastructure.Persistence.Models;

namespace NIU.ACH_AI.Infrastructure.Persistence.Tests.Mappers;

/// <summary>
/// Unit tests for EvidenceMapper following FIRST principles.
/// Tests are Fast, Isolated, Repeatable, Self-validating, and Timely.
/// </summary>
public class EvidenceMapperTests
{
    #region ToDatabase Tests

    /// <summary>
    /// Verifies that mapping a domain evidence with an empty GUID to database generates a new unique GUID.
    /// </summary>
    [Fact]
    public void ToDatabase_WithEmptyEvidenceId_GeneratesNewGuid()
    {
        // Arrange
        var domainEvidence = new DomainEntity.Evidence
        {
            EvidenceId = Guid.Empty,
            Claim = "Test claim",
            ReferenceSnippet = "Test snippet",
            Type = EvidenceType.Fact,
            Notes = "Test notes"
        };
        var stepExecutionId = Guid.NewGuid();

        // Act
        var result = EvidenceMapper.ToDatabase(domainEvidence, stepExecutionId);

        // Assert
        result.EvidenceId.Should().NotBe(Guid.Empty);
        result.StepExecutionId.Should().Be(stepExecutionId);
        result.Claim.Should().Be("Test claim");
        result.ReferenceSnippet.Should().Be("Test snippet");
        result.EvidenceTypeId.Should().Be((int)EvidenceType.Fact);
        result.Notes.Should().Be("Test notes");
        result.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    /// <summary>
    /// Verifies that mapping a domain evidence with an existing GUID preserves that GUID in the database entity.
    /// </summary>
    [Fact]
    public void ToDatabase_WithExistingEvidenceId_GeneratesNewId()
    {
        // Arrange
        var existingGuid = Guid.NewGuid();
        var domainEvidence = new DomainEntity.Evidence
        {
            EvidenceId = existingGuid,
            Claim = "Test claim",
            ReferenceSnippet = "Test snippet",
            Type = EvidenceType.Assumption,
            Notes = "Test notes"
        };
        var stepExecutionId = Guid.NewGuid();

        // Act
        var result = EvidenceMapper.ToDatabase(domainEvidence, stepExecutionId);

        // Assert
        result.EvidenceId.Should().NotBe(existingGuid);
        result.EvidenceId.Should().NotBe(Guid.Empty);
    }

    /// <summary>
    /// Verifies that null notes in a domain evidence are converted to an empty string when mapping to database.
    /// </summary>
    [Fact]
    public void ToDatabase_WithNullNotes_ConvertsToEmptyString()
    {
        // Arrange
        var domainEvidence = new DomainEntity.Evidence
        {
            EvidenceId = Guid.NewGuid(),
            Claim = "Test claim",
            ReferenceSnippet = "Test snippet",
            Type = EvidenceType.ExpertOpinion,
            Notes = null!
        };
        var stepExecutionId = Guid.NewGuid();

        // Act
        var result = EvidenceMapper.ToDatabase(domainEvidence, stepExecutionId);

        // Assert
        result.Notes.Should().Be(string.Empty);
    }

    /// <summary>
    /// Verifies that a null reference snippet in domain evidence remains null when mapping to database.
    /// </summary>
    [Fact]
    public void ToDatabase_WithNullReferenceSnippet_PreservesNull()
    {
        // Arrange
        var domainEvidence = new DomainEntity.Evidence
        {
            EvidenceId = Guid.NewGuid(),
            Claim = "Test claim",
            ReferenceSnippet = null,
            Type = EvidenceType.Fact,
            Notes = "Test notes"
        };
        var stepExecutionId = Guid.NewGuid();

        // Act
        var result = EvidenceMapper.ToDatabase(domainEvidence, stepExecutionId);

        // Assert
        result.ReferenceSnippet.Should().BeNull();
    }

    /// <summary>
    /// Verifies that all evidence type enums (Fact, Assumption, ExpertOpinion) map correctly to their database integer IDs.
    /// </summary>
    [Theory]
    [InlineData(EvidenceType.Fact, 1)]
    [InlineData(EvidenceType.Assumption, 2)]
    [InlineData(EvidenceType.ExpertOpinion, 3)]
    public void ToDatabase_WithDifferentEvidenceTypes_MapsCorrectly(EvidenceType type, int expectedId)
    {
        // Arrange
        var domainEvidence = new DomainEntity.Evidence
        {
            EvidenceId = Guid.NewGuid(),
            Claim = "Test claim",
            ReferenceSnippet = "Test snippet",
            Type = type,
            Notes = "Test notes"
        };
        var stepExecutionId = Guid.NewGuid();

        // Act
        var result = EvidenceMapper.ToDatabase(domainEvidence, stepExecutionId);

        // Assert
        result.EvidenceTypeId.Should().Be(expectedId);
    }

    /// <summary>
    /// Verifies that empty strings in domain evidence properties are preserved as empty strings in the database entity.
    /// </summary>
    [Fact]
    public void ToDatabase_WithEmptyStrings_PreservesEmptyStrings()
    {
        // Arrange
        var domainEvidence = new DomainEntity.Evidence
        {
            EvidenceId = Guid.NewGuid(),
            Claim = "",
            ReferenceSnippet = "",
            Type = EvidenceType.Fact,
            Notes = ""
        };
        var stepExecutionId = Guid.NewGuid();

        // Act
        var result = EvidenceMapper.ToDatabase(domainEvidence, stepExecutionId);

        // Assert
        result.Claim.Should().Be("");
        result.ReferenceSnippet.Should().Be("");
        result.Notes.Should().Be("");
    }

    /// <summary>
    /// Verifies that very long strings (5000+ characters) are fully preserved without truncation when mapping to database.
    /// </summary>
    [Fact]
    public void ToDatabase_WithLongStrings_PreservesFullContent()
    {
        // Arrange
        var longClaim = new string('A', 5000);
        var longSnippet = new string('B', 5000);
        var longNotes = new string('C', 5000);
        
        var domainEvidence = new DomainEntity.Evidence
        {
            EvidenceId = Guid.NewGuid(),
            Claim = longClaim,
            ReferenceSnippet = longSnippet,
            Type = EvidenceType.Fact,
            Notes = longNotes
        };
        var stepExecutionId = Guid.NewGuid();

        // Act
        var result = EvidenceMapper.ToDatabase(domainEvidence, stepExecutionId);

        // Assert
        result.Claim.Should().Be(longClaim);
        result.ReferenceSnippet.Should().Be(longSnippet);
        result.Notes.Should().Be(longNotes);
    }

    #endregion

    #region ToDomain Tests

    /// <summary>
    /// Verifies that all properties of a database evidence entity are correctly mapped to a domain evidence object.
    /// </summary>
    [Fact]
    public void ToDomain_WithValidDatabaseEntity_MapsAllProperties()
    {
        // Arrange
        var evidenceId = Guid.NewGuid();
        var dbEvidence = new DbModel.Evidence
        {
            EvidenceId = evidenceId,
            StepExecutionId = Guid.NewGuid(),
            Claim = "Test claim",
            ReferenceSnippet = "Test snippet",
            EvidenceTypeId = (int)EvidenceType.Fact,
            Notes = "Test notes",
            CreatedAt = DateTime.UtcNow
        };

        // Act
        var result = EvidenceMapper.ToDomain(dbEvidence);

        // Assert
        result.EvidenceId.Should().Be(evidenceId);
        result.Claim.Should().Be("Test claim");
        result.ReferenceSnippet.Should().Be("Test snippet");
        result.Type.Should().Be(EvidenceType.Fact);
        result.Notes.Should().Be("Test notes");
    }

    /// <summary>
    /// Verifies that null notes in a database evidence are converted to an empty string when mapping to domain.
    /// </summary>
    [Fact]
    public void ToDomain_WithNullNotes_ConvertsToEmptyString()
    {
        // Arrange
        var dbEvidence = new DbModel.Evidence
        {
            EvidenceId = Guid.NewGuid(),
            StepExecutionId = Guid.NewGuid(),
            Claim = "Test claim",
            ReferenceSnippet = "Test snippet",
            EvidenceTypeId = (int)EvidenceType.Assumption,
            Notes = null,
            CreatedAt = DateTime.UtcNow
        };

        // Act
        var result = EvidenceMapper.ToDomain(dbEvidence);

        // Assert
        result.Notes.Should().Be(string.Empty);
    }

    /// <summary>
    /// Verifies that a null reference snippet in database evidence remains null when mapping to domain.
    /// </summary>
    [Fact]
    public void ToDomain_WithNullReferenceSnippet_PreservesNull()
    {
        // Arrange
        var dbEvidence = new DbModel.Evidence
        {
            EvidenceId = Guid.NewGuid(),
            StepExecutionId = Guid.NewGuid(),
            Claim = "Test claim",
            ReferenceSnippet = null,
            EvidenceTypeId = (int)EvidenceType.ExpertOpinion,
            Notes = "Test notes",
            CreatedAt = DateTime.UtcNow
        };

        // Act
        var result = EvidenceMapper.ToDomain(dbEvidence);

        // Assert
        result.ReferenceSnippet.Should().BeNull();
    }

    /// <summary>
    /// Verifies that database integer IDs correctly map back to their corresponding evidence type enums.
    /// </summary>
    [Theory]
    [InlineData(1, EvidenceType.Fact)]
    [InlineData(2, EvidenceType.Assumption)]
    [InlineData(3, EvidenceType.ExpertOpinion)]
    public void ToDomain_WithDifferentEvidenceTypeIds_MapsCorrectly(int typeId, EvidenceType expectedType)
    {
        // Arrange
        var dbEvidence = new DbModel.Evidence
        {
            EvidenceId = Guid.NewGuid(),
            StepExecutionId = Guid.NewGuid(),
            Claim = "Test claim",
            ReferenceSnippet = "Test snippet",
            EvidenceTypeId = typeId,
            Notes = "Test notes",
            CreatedAt = DateTime.UtcNow
        };

        // Act
        var result = EvidenceMapper.ToDomain(dbEvidence);

        // Assert
        result.Type.Should().Be(expectedType);
    }

    #endregion

    #region ToDomain Collection Tests

    /// <summary>
    /// Verifies that mapping an empty collection of database evidence entities returns an empty list, not null.
    /// </summary>
    [Fact]
    public void ToDomain_WithEmptyCollection_ReturnsEmptyList()
    {
        // Arrange
        var dbEntities = new List<DbModel.Evidence>();

        // Act
        var result = EvidenceMapper.ToDomain(dbEntities);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    /// <summary>
    /// Verifies that mapping multiple database evidence entities correctly converts all items to domain objects with proper property values.
    /// </summary>
    [Fact]
    public void ToDomain_WithMultipleEntities_MapsAllCorrectly()
    {
        // Arrange
        var dbEntities = new List<DbModel.Evidence>
        {
            new DbModel.Evidence
            {
                EvidenceId = Guid.NewGuid(),
                StepExecutionId = Guid.NewGuid(),
                Claim = "Claim 1",
                ReferenceSnippet = "Snippet 1",
                EvidenceTypeId = (int)EvidenceType.Fact,
                Notes = "Notes 1",
                CreatedAt = DateTime.UtcNow
            },
            new DbModel.Evidence
            {
                EvidenceId = Guid.NewGuid(),
                StepExecutionId = Guid.NewGuid(),
                Claim = "Claim 2",
                ReferenceSnippet = "Snippet 2",
                EvidenceTypeId = (int)EvidenceType.Assumption,
                Notes = "Notes 2",
                CreatedAt = DateTime.UtcNow
            },
            new DbModel.Evidence
            {
                EvidenceId = Guid.NewGuid(),
                StepExecutionId = Guid.NewGuid(),
                Claim = "Claim 3",
                ReferenceSnippet = "Snippet 3",
                EvidenceTypeId = (int)EvidenceType.ExpertOpinion,
                Notes = "Notes 3",
                CreatedAt = DateTime.UtcNow
            }
        };

        // Act
        var result = EvidenceMapper.ToDomain(dbEntities);

        // Assert
        result.Should().HaveCount(3);
        result[0].Claim.Should().Be("Claim 1");
        result[0].Type.Should().Be(EvidenceType.Fact);
        result[1].Claim.Should().Be("Claim 2");
        result[1].Type.Should().Be(EvidenceType.Assumption);
        result[2].Claim.Should().Be("Claim 3");
        result[2].Type.Should().Be(EvidenceType.ExpertOpinion);
    }

    /// <summary>
    /// Verifies that mapping a collection with one database evidence entity correctly returns a single domain object.
    /// </summary>
    [Fact]
    public void ToDomain_WithSingleItemCollection_MapsSingleItem()
    {
        // Arrange
        var evidenceId = Guid.NewGuid();
        var dbEntities = new List<DbModel.Evidence>
        {
            new DbModel.Evidence
            {
                EvidenceId = evidenceId,
                StepExecutionId = Guid.NewGuid(),
                Claim = "Single claim",
                ReferenceSnippet = "Single snippet",
                EvidenceTypeId = (int)EvidenceType.Fact,
                Notes = "Single notes",
                CreatedAt = DateTime.UtcNow
            }
        };

        // Act
        var result = EvidenceMapper.ToDomain(dbEntities);

        // Assert
        result.Should().HaveCount(1);
        result[0].EvidenceId.Should().Be(evidenceId);
        result[0].Claim.Should().Be("Single claim");
    }

    #endregion

    #region Round-Trip Tests

    /// <summary>
    /// Verifies that converting domain to database and back to domain preserves all properties correctly (round-trip mapping).
    /// </summary>
    [Fact]
    public void RoundTrip_ToDatabaseThenToDomain_PreservesNonGuidProperties()
    {
        // Arrange
        var originalDomain = new DomainEntity.Evidence
        {
            EvidenceId = Guid.NewGuid(),
            Claim = "Round trip claim",
            ReferenceSnippet = "Round trip snippet",
            Type = EvidenceType.Assumption,
            Notes = "Round trip notes"
        };
        var stepExecutionId = Guid.NewGuid();

        // Act
        var dbEntity = EvidenceMapper.ToDatabase(originalDomain, stepExecutionId);
        var resultDomain = EvidenceMapper.ToDomain(dbEntity);

        // Assert
        resultDomain.EvidenceId.Should().NotBe(originalDomain.EvidenceId);
        resultDomain.Claim.Should().Be(originalDomain.Claim);
        resultDomain.ReferenceSnippet.Should().Be(originalDomain.ReferenceSnippet);
        resultDomain.Type.Should().Be(originalDomain.Type);
        resultDomain.Notes.Should().Be(originalDomain.Notes);
    }

    /// <summary>
    /// Verifies that a round-trip mapping with an empty evidence ID generates a new GUID that differs from the original empty GUID.
    /// </summary>
    [Fact]
    public void RoundTrip_WithEmptyEvidenceId_GeneratesNewGuid()
    {
        // Arrange
        var originalDomain = new DomainEntity.Evidence
        {
            EvidenceId = Guid.Empty,
            Claim = "Round trip claim",
            ReferenceSnippet = "Round trip snippet",
            Type = EvidenceType.Fact,
            Notes = "Round trip notes"
        };
        var stepExecutionId = Guid.NewGuid();

        // Act
        var dbEntity = EvidenceMapper.ToDatabase(originalDomain, stepExecutionId);
        var resultDomain = EvidenceMapper.ToDomain(dbEntity);

        // Assert
        resultDomain.EvidenceId.Should().NotBe(Guid.Empty);
        resultDomain.EvidenceId.Should().NotBe(originalDomain.EvidenceId);
    }

    #endregion
}
