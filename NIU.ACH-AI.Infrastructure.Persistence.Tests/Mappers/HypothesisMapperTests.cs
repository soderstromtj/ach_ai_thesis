using FluentAssertions;
using NIU.ACH_AI.Infrastructure.Persistence.Mappers;
using DomainEntity = NIU.ACH_AI.Domain.Entities;
using DbModel = NIU.ACH_AI.Infrastructure.Persistence.Models;

namespace NIU.ACH_AI.Infrastructure.Persistence.Tests.Mappers;

/// <summary>
/// Unit tests for HypothesisMapper following FIRST principles.
/// Tests are Fast, Isolated, Repeatable, Self-validating, and Timely.
/// </summary>
public class HypothesisMapperTests
{
    #region ToDatabase Tests

    /// <summary>
    /// Verifies that mapping a domain hypothesis to database generates a new unique GUID and sets all properties correctly.
    /// </summary>
    [Fact]
    public void ToDatabase_WithValidDomainEntity_GeneratesNewGuid()
    {
        // Arrange
        var domainHypothesis = new DomainEntity.Hypothesis
        {
            ShortTitle = "Test Hypothesis",
            HypothesisText = "This is a test hypothesis"
        };
        var stepExecutionId = Guid.NewGuid();

        // Act
        var result = HypothesisMapper.ToDatabase(domainHypothesis, stepExecutionId);

        // Assert
        result.HypothesisId.Should().NotBe(Guid.Empty);
        result.StepExecutionId.Should().Be(stepExecutionId);
        result.ShortTitle.Should().Be("Test Hypothesis");
        result.HypothesisText.Should().Be("This is a test hypothesis");
        result.IsRefined.Should().BeFalse();
        result.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    /// <summary>
    /// Verifies that each mapping operation generates a unique GUID, even for the same domain hypothesis.
    /// </summary>
    [Fact]
    public void ToDatabase_AlwaysGeneratesNewGuid()
    {
        // Arrange
        var domainHypothesis = new DomainEntity.Hypothesis
        {
            ShortTitle = "Test Hypothesis",
            HypothesisText = "This is a test hypothesis"
        };
        var stepExecutionId = Guid.NewGuid();

        // Act
        var result1 = HypothesisMapper.ToDatabase(domainHypothesis, stepExecutionId);
        var result2 = HypothesisMapper.ToDatabase(domainHypothesis, stepExecutionId);

        // Assert
        result1.HypothesisId.Should().NotBe(Guid.Empty);
        result2.HypothesisId.Should().NotBe(Guid.Empty);
        result1.HypothesisId.Should().NotBe(result2.HypothesisId);
    }

    /// <summary>
    /// Verifies that empty strings in hypothesis properties are preserved when mapping to database.
    /// </summary>
    [Fact]
    public void ToDatabase_WithEmptyStrings_PreservesEmptyStrings()
    {
        // Arrange
        var domainHypothesis = new DomainEntity.Hypothesis
        {
            ShortTitle = "",
            HypothesisText = ""
        };
        var stepExecutionId = Guid.NewGuid();

        // Act
        var result = HypothesisMapper.ToDatabase(domainHypothesis, stepExecutionId);

        // Assert
        result.ShortTitle.Should().Be("");
        result.HypothesisText.Should().Be("");
    }

    /// <summary>
    /// Verifies that very long strings are fully preserved without truncation when mapping to database.
    /// </summary>
    [Fact]
    public void ToDatabase_WithLongStrings_PreservesFullContent()
    {
        // Arrange
        var longTitle = new string('A', 500);
        var longText = new string('B', 10000);
        
        var domainHypothesis = new DomainEntity.Hypothesis
        {
            ShortTitle = longTitle,
            HypothesisText = longText
        };
        var stepExecutionId = Guid.NewGuid();

        // Act
        var result = HypothesisMapper.ToDatabase(domainHypothesis, stepExecutionId);

        // Assert
        result.ShortTitle.Should().Be(longTitle);
        result.HypothesisText.Should().Be(longText);
    }

    /// <summary>
    /// Verifies that the IsRefined flag defaults to false when not explicitly specified.
    /// </summary>
    [Fact]
    public void ToDatabase_WithoutIsRefinedParameter_DefaultsToFalse()
    {
        // Arrange
        var domainHypothesis = new DomainEntity.Hypothesis
        {
            ShortTitle = "Test Hypothesis",
            HypothesisText = "This is a test hypothesis"
        };
        var stepExecutionId = Guid.NewGuid();

        // Act
        var result = HypothesisMapper.ToDatabase(domainHypothesis, stepExecutionId);

        // Assert
        result.IsRefined.Should().BeFalse();
    }

    /// <summary>
    /// Verifies that the IsRefined flag can be explicitly set to true when mapping to database.
    /// This tests the core functionality added in commit 9337223d.
    /// </summary>
    [Fact]
    public void ToDatabase_WithIsRefinedTrue_SetsIsRefinedToTrue()
    {
        // Arrange
        var domainHypothesis = new DomainEntity.Hypothesis
        {
            ShortTitle = "Refined Hypothesis",
            HypothesisText = "This hypothesis has been refined"
        };
        var stepExecutionId = Guid.NewGuid();

        // Act
        var result = HypothesisMapper.ToDatabase(domainHypothesis, stepExecutionId, isRefined: true);

        // Assert
        result.IsRefined.Should().BeTrue();
        result.ShortTitle.Should().Be("Refined Hypothesis");
        result.HypothesisText.Should().Be("This hypothesis has been refined");
    }

    /// <summary>
    /// Verifies that the IsRefined flag can be explicitly set to false when mapping to database.
    /// </summary>
    [Fact]
    public void ToDatabase_WithIsRefinedFalse_SetsIsRefinedToFalse()
    {
        // Arrange
        var domainHypothesis = new DomainEntity.Hypothesis
        {
            ShortTitle = "Unrefined Hypothesis",
            HypothesisText = "This hypothesis has not been refined"
        };
        var stepExecutionId = Guid.NewGuid();

        // Act
        var result = HypothesisMapper.ToDatabase(domainHypothesis, stepExecutionId, isRefined: false);

        // Assert
        result.IsRefined.Should().BeFalse();
        result.ShortTitle.Should().Be("Unrefined Hypothesis");
        result.HypothesisText.Should().Be("This hypothesis has not been refined");
    }

    /// <summary>
    /// Verifies that different isRefined values produce different database entities.
    /// Tests the parameterization capability added for hypothesis refinement tracking.
    /// </summary>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ToDatabase_WithDifferentIsRefinedValues_StoresCorrectly(bool isRefined)
    {
        // Arrange
        var domainHypothesis = new DomainEntity.Hypothesis
        {
            ShortTitle = "Parameterized Test Hypothesis",
            HypothesisText = "Testing isRefined parameter"
        };
        var stepExecutionId = Guid.NewGuid();

        // Act
        var result = HypothesisMapper.ToDatabase(domainHypothesis, stepExecutionId, isRefined);

        // Assert
        result.IsRefined.Should().Be(isRefined);
        result.ShortTitle.Should().Be("Parameterized Test Hypothesis");
        result.HypothesisText.Should().Be("Testing isRefined parameter");
    }

    /// <summary>
    /// Verifies that special characters like &lt;, &gt;, &amp;, and quotes are preserved when mapping to database.
    /// </summary>
    [Fact]
    public void ToDatabase_WithSpecialCharacters_PreservesCharacters()
    {
        // Arrange
        var domainHypothesis = new DomainEntity.Hypothesis
        {
            ShortTitle = "Test: <Special> & \"Characters\"",
            HypothesisText = "Hypothesis with special chars: @#$%^&*()"
        };
        var stepExecutionId = Guid.NewGuid();

        // Act
        var result = HypothesisMapper.ToDatabase(domainHypothesis, stepExecutionId);

        // Assert
        result.ShortTitle.Should().Be("Test: <Special> & \"Characters\"");
        result.HypothesisText.Should().Be("Hypothesis with special chars: @#$%^&*()");
    }

    /// <summary>
    /// Verifies that Unicode characters including Chinese, Cyrillic, and emojis are preserved when mapping to database.
    /// </summary>
    [Fact]
    public void ToDatabase_WithUnicodeCharacters_PreservesUnicode()
    {
        // Arrange
        var domainHypothesis = new DomainEntity.Hypothesis
        {
            ShortTitle = "测试假设",
            HypothesisText = "Это тестовая гипотеза 🧪"
        };
        var stepExecutionId = Guid.NewGuid();

        // Act
        var result = HypothesisMapper.ToDatabase(domainHypothesis, stepExecutionId);

        // Assert
        result.ShortTitle.Should().Be("测试假设");
        result.HypothesisText.Should().Be("Это тестовая гипотеза 🧪");
    }

    #endregion

    #region ToDomain Tests

    /// <summary>
    /// Verifies that all text properties of a database hypothesis are correctly mapped to a domain hypothesis.
    /// </summary>
    [Fact]
    public void ToDomain_WithValidDatabaseEntity_MapsAllProperties()
    {
        // Arrange
        var dbHypothesis = new DbModel.Hypothesis
        {
            HypothesisId = Guid.NewGuid(),
            StepExecutionId = Guid.NewGuid(),
            ShortTitle = "Test Hypothesis",
            HypothesisText = "This is a test hypothesis",
            IsRefined = false,
            CreatedAt = DateTime.UtcNow
        };

        // Act
        var result = HypothesisMapper.ToDomain(dbHypothesis);

        // Assert
        result.ShortTitle.Should().Be("Test Hypothesis");
        result.HypothesisText.Should().Be("This is a test hypothesis");
    }

    /// <summary>
    /// Verifies that the hypothesis ID is not mapped to the domain entity (as it doesn't exist in the domain model).
    /// </summary>
    [Fact]
    public void ToDomain_DoesNotMapHypothesisId()
    {
        // Arrange
        var hypothesisId = Guid.NewGuid();
        var dbHypothesis = new DbModel.Hypothesis
        {
            HypothesisId = hypothesisId,
            StepExecutionId = Guid.NewGuid(),
            ShortTitle = "Test Hypothesis",
            HypothesisText = "This is a test hypothesis",
            IsRefined = true,
            CreatedAt = DateTime.UtcNow
        };

        // Act
        var result = HypothesisMapper.ToDomain(dbHypothesis);

        // Assert - Domain entity doesn't have HypothesisId property in the mapping
        result.ShortTitle.Should().Be("Test Hypothesis");
        result.HypothesisText.Should().Be("This is a test hypothesis");
    }

    /// <summary>
    /// Verifies that empty strings are preserved when mapping from database to domain.
    /// </summary>
    [Fact]
    public void ToDomain_WithEmptyStrings_PreservesEmptyStrings()
    {
        // Arrange
        var dbHypothesis = new DbModel.Hypothesis
        {
            HypothesisId = Guid.NewGuid(),
            StepExecutionId = Guid.NewGuid(),
            ShortTitle = "",
            HypothesisText = "",
            IsRefined = false,
            CreatedAt = DateTime.UtcNow
        };

        // Act
        var result = HypothesisMapper.ToDomain(dbHypothesis);

        // Assert
        result.ShortTitle.Should().Be("");
        result.HypothesisText.Should().Be("");
    }

    /// <summary>
    /// Verifies that special characters are preserved when mapping from database to domain.
    /// </summary>
    [Fact]
    public void ToDomain_WithSpecialCharacters_PreservesCharacters()
    {
        // Arrange
        var dbHypothesis = new DbModel.Hypothesis
        {
            HypothesisId = Guid.NewGuid(),
            StepExecutionId = Guid.NewGuid(),
            ShortTitle = "Test: <Special> & \"Characters\"",
            HypothesisText = "Hypothesis with special chars: @#$%^&*()",
            IsRefined = false,
            CreatedAt = DateTime.UtcNow
        };

        // Act
        var result = HypothesisMapper.ToDomain(dbHypothesis);

        // Assert
        result.ShortTitle.Should().Be("Test: <Special> & \"Characters\"");
        result.HypothesisText.Should().Be("Hypothesis with special chars: @#$%^&*()");
    }

    #endregion

    #region ToDomain Collection Tests

    /// <summary>
    /// Verifies that mapping an empty collection returns an empty list, not null.
    /// </summary>
    [Fact]
    public void ToDomain_WithEmptyCollection_ReturnsEmptyList()
    {
        // Arrange
        var dbEntities = new List<DbModel.Hypothesis>();

        // Act
        var result = HypothesisMapper.ToDomain(dbEntities);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    /// <summary>
    /// Verifies that mapping multiple database hypotheses correctly converts all items to domain objects.
    /// </summary>
    [Fact]
    public void ToDomain_WithMultipleEntities_MapsAllCorrectly()
    {
        // Arrange
        var dbEntities = new List<DbModel.Hypothesis>
        {
            new DbModel.Hypothesis
            {
                HypothesisId = Guid.NewGuid(),
                StepExecutionId = Guid.NewGuid(),
                ShortTitle = "Hypothesis 1",
                HypothesisText = "Text 1",
                IsRefined = false,
                CreatedAt = DateTime.UtcNow
            },
            new DbModel.Hypothesis
            {
                HypothesisId = Guid.NewGuid(),
                StepExecutionId = Guid.NewGuid(),
                ShortTitle = "Hypothesis 2",
                HypothesisText = "Text 2",
                IsRefined = true,
                CreatedAt = DateTime.UtcNow
            },
            new DbModel.Hypothesis
            {
                HypothesisId = Guid.NewGuid(),
                StepExecutionId = Guid.NewGuid(),
                ShortTitle = "Hypothesis 3",
                HypothesisText = "Text 3",
                IsRefined = false,
                CreatedAt = DateTime.UtcNow
            }
        };

        // Act
        var result = HypothesisMapper.ToDomain(dbEntities);

        // Assert
        result.Should().HaveCount(3);
        result[0].ShortTitle.Should().Be("Hypothesis 1");
        result[0].HypothesisText.Should().Be("Text 1");
        result[1].ShortTitle.Should().Be("Hypothesis 2");
        result[1].HypothesisText.Should().Be("Text 2");
        result[2].ShortTitle.Should().Be("Hypothesis 3");
        result[2].HypothesisText.Should().Be("Text 3");
    }

    /// <summary>
    /// Verifies that mapping a single-item collection correctly returns one domain hypothesis.
    /// </summary>
    [Fact]
    public void ToDomain_WithSingleItemCollection_MapsSingleItem()
    {
        // Arrange
        var dbEntities = new List<DbModel.Hypothesis>
        {
            new DbModel.Hypothesis
            {
                HypothesisId = Guid.NewGuid(),
                StepExecutionId = Guid.NewGuid(),
                ShortTitle = "Single Hypothesis",
                HypothesisText = "Single text",
                IsRefined = false,
                CreatedAt = DateTime.UtcNow
            }
        };

        // Act
        var result = HypothesisMapper.ToDomain(dbEntities);

        // Assert
        result.Should().HaveCount(1);
        result[0].ShortTitle.Should().Be("Single Hypothesis");
        result[0].HypothesisText.Should().Be("Single text");
    }

    #endregion

    #region Round-Trip Tests

    /// <summary>
    /// Verifies that converting domain to database and back preserves text properties (round-trip mapping).
    /// </summary>
    [Fact]
    public void RoundTrip_ToDatabaseThenToDomain_PreservesTextProperties()
    {
        // Arrange
        var originalDomain = new DomainEntity.Hypothesis
        {
            ShortTitle = "Round Trip Hypothesis",
            HypothesisText = "Round trip hypothesis text"
        };
        var stepExecutionId = Guid.NewGuid();

        // Act
        var dbEntity = HypothesisMapper.ToDatabase(originalDomain, stepExecutionId);
        var resultDomain = HypothesisMapper.ToDomain(dbEntity);

        // Assert
        resultDomain.ShortTitle.Should().Be(originalDomain.ShortTitle);
        resultDomain.HypothesisText.Should().Be(originalDomain.HypothesisText);
    }

    /// <summary>
    /// Verifies that multiple round-trip conversions produce consistent results without data loss.
    /// </summary>
    [Fact]
    public void RoundTrip_MultipleTimes_ProducesConsistentResults()
    {
        // Arrange
        var originalDomain = new DomainEntity.Hypothesis
        {
            ShortTitle = "Consistent Hypothesis",
            HypothesisText = "Consistent hypothesis text"
        };
        var stepExecutionId = Guid.NewGuid();

        // Act
        var dbEntity1 = HypothesisMapper.ToDatabase(originalDomain, stepExecutionId);
        var domainEntity1 = HypothesisMapper.ToDomain(dbEntity1);
        
        var dbEntity2 = HypothesisMapper.ToDatabase(domainEntity1, stepExecutionId);
        var domainEntity2 = HypothesisMapper.ToDomain(dbEntity2);

        // Assert
        domainEntity1.ShortTitle.Should().Be(domainEntity2.ShortTitle);
        domainEntity1.HypothesisText.Should().Be(domainEntity2.HypothesisText);
    }

    #endregion
}
