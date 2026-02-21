using NIU.ACH_AI.Domain.Entities;
using NIU.ACH_AI.Domain.ValueObjects;

namespace NIU.ACH_AI.Infrastructure.Tests.Domain;

/// <summary>
/// Unit tests for Evidence entity.
///
/// Testing Strategy:
/// -----------------
/// Evidence is a domain entity with properties and a ToString() method.
///
/// Key testing areas:
/// 1. Default values
/// 2. Property assignment
/// 3. ToString formatting
/// 4. EvidenceType handling
/// </summary>
public class EvidenceTests
{
    #region Default Value Tests

    /// <summary>
    /// WHY: Verifies default instance has proper defaults.
    /// </summary>
    [Fact]
    public void NewInstance_HasDefaultValues()
    {
        // Arrange & Act
        var evidence = new Evidence();

        // Assert
        Assert.Equal(Guid.Empty, evidence.EvidenceId);
        Assert.Equal(string.Empty, evidence.Claim);
        Assert.Null(evidence.ReferenceSnippet);
        Assert.Equal(default(EvidenceType), evidence.Type);
        Assert.Equal(string.Empty, evidence.Notes);
    }

    /// <summary>
    /// WHY: Verifies Claim has empty string default.
    /// </summary>
    [Fact]
    public void Claim_DefaultsToEmptyString()
    {
        // Arrange & Act
        var evidence = new Evidence();

        // Assert
        Assert.Equal(string.Empty, evidence.Claim);
    }

    /// <summary>
    /// WHY: Verifies Notes has empty string default.
    /// </summary>
    [Fact]
    public void Notes_DefaultsToEmptyString()
    {
        // Arrange & Act
        var evidence = new Evidence();

        // Assert
        Assert.Equal(string.Empty, evidence.Notes);
    }

    /// <summary>
    /// WHY: Verifies ReferenceSnippet is nullable.
    /// </summary>
    [Fact]
    public void ReferenceSnippet_IsNullByDefault()
    {
        // Arrange & Act
        var evidence = new Evidence();

        // Assert
        Assert.Null(evidence.ReferenceSnippet);
    }

    #endregion

    #region Property Assignment Tests

    /// <summary>
    /// WHY: Verifies all properties can be assigned.
    /// </summary>
    [Fact]
    public void Properties_CanBeAssigned()
    {
        // Arrange
        var id = Guid.NewGuid();

        // Act
        var evidence = new Evidence
        {
            EvidenceId = id,
            Claim = "Test claim",
            ReferenceSnippet = "Source reference",
            Type = EvidenceType.VerifiableFact,
            Notes = "Additional notes"
        };

        // Assert
        Assert.Equal(id, evidence.EvidenceId);
        Assert.Equal("Test claim", evidence.Claim);
        Assert.Equal("Source reference", evidence.ReferenceSnippet);
        Assert.Equal(EvidenceType.VerifiableFact, evidence.Type);
        Assert.Equal("Additional notes", evidence.Notes);
    }

    /// <summary>
    /// WHY: Verifies EvidenceId can be set.
    /// </summary>
    [Fact]
    public void EvidenceId_CanBeSet()
    {
        // Arrange
        var id = Guid.NewGuid();
        var evidence = new Evidence();

        // Act
        evidence.EvidenceId = id;

        // Assert
        Assert.Equal(id, evidence.EvidenceId);
    }

    /// <summary>
    /// WHY: Verifies EvidenceType.Fact can be set.
    /// </summary>
    [Fact]
    public void Type_CanBeSetToFact()
    {
        // Arrange
        var evidence = new Evidence();

        // Act
        evidence.Type = EvidenceType.VerifiableFact;

        // Assert
        Assert.Equal(EvidenceType.VerifiableFact, evidence.Type);
    }

    /// <summary>
    /// WHY: Verifies EvidenceType.Assumption can be set.
    /// </summary>
    [Fact]
    public void Type_CanBeSetToAssumption()
    {
        // Arrange
        var evidence = new Evidence();

        // Act
        evidence.Type = EvidenceType.StatedAssumption;

        // Assert
        Assert.Equal(EvidenceType.StatedAssumption, evidence.Type);
    }

    #endregion

    #region ToString - Basic Tests

    /// <summary>
    /// WHY: Verifies ToString returns non-empty string.
    /// </summary>
    [Fact]
    public void ToString_Always_ReturnsNonEmptyString()
    {
        // Arrange
        var evidence = new Evidence();

        // Act
        var result = evidence.ToString();

        // Assert
        Assert.False(string.IsNullOrWhiteSpace(result));
    }

    /// <summary>
    /// WHY: Verifies ToString includes EvidenceId label.
    /// </summary>
    [Fact]
    public void ToString_IncludesEvidenceIdLabel()
    {
        // Arrange
        var evidence = new Evidence { EvidenceId = Guid.NewGuid() };

        // Act
        var result = evidence.ToString();

        // Assert
        Assert.Contains("EvidenceId:", result);
    }

    /// <summary>
    /// WHY: Verifies ToString includes Claim label.
    /// </summary>
    [Fact]
    public void ToString_IncludesClaimLabel()
    {
        // Arrange
        var evidence = new Evidence { Claim = "Test claim" };

        // Act
        var result = evidence.ToString();

        // Assert
        Assert.Contains("Claim:", result);
        Assert.Contains("Test claim", result);
    }

    /// <summary>
    /// WHY: Verifies ToString includes Type label.
    /// </summary>
    [Fact]
    public void ToString_IncludesTypeLabel()
    {
        // Arrange
        var evidence = new Evidence { Type = EvidenceType.VerifiableFact };

        // Act
        var result = evidence.ToString();

        // Assert
        Assert.Contains("Type:", result);
        Assert.Contains("Fact", result);
    }

    /// <summary>
    /// WHY: Verifies ToString includes Notes label.
    /// </summary>
    [Fact]
    public void ToString_IncludesNotesLabel()
    {
        // Arrange
        var evidence = new Evidence { Notes = "Important notes" };

        // Act
        var result = evidence.ToString();

        // Assert
        Assert.Contains("Notes:", result);
        Assert.Contains("Important notes", result);
    }

    /// <summary>
    /// WHY: Verifies ToString includes ReferenceSnippet label.
    /// </summary>
    [Fact]
    public void ToString_IncludesReferenceSnippetLabel()
    {
        // Arrange
        var evidence = new Evidence { ReferenceSnippet = "Source text" };

        // Act
        var result = evidence.ToString();

        // Assert
        Assert.Contains("ReferenceSnippet:", result);
        Assert.Contains("Source text", result);
    }

    #endregion

    #region ToString - Content Tests

    /// <summary>
    /// WHY: Verifies ToString includes actual EvidenceId value.
    /// </summary>
    [Fact]
    public void ToString_IncludesEvidenceIdValue()
    {
        // Arrange
        var id = Guid.NewGuid();
        var evidence = new Evidence { EvidenceId = id };

        // Act
        var result = evidence.ToString();

        // Assert
        Assert.Contains(id.ToString(), result);
    }

    /// <summary>
    /// WHY: Verifies ToString displays Fact type correctly.
    /// </summary>
    [Fact]
    public void ToString_WithFactType_DisplaysFact()
    {
        // Arrange
        var evidence = new Evidence { Type = EvidenceType.VerifiableFact };

        // Act
        var result = evidence.ToString();

        // Assert
        Assert.Contains("Fact", result);
    }

    /// <summary>
    /// WHY: Verifies ToString displays Assumption type correctly.
    /// </summary>
    [Fact]
    public void ToString_WithAssumptionType_DisplaysAssumption()
    {
        // Arrange
        var evidence = new Evidence { Type = EvidenceType.StatedAssumption };

        // Act
        var result = evidence.ToString();

        // Assert
        Assert.Contains("Assumption", result);
    }

    /// <summary>
    /// WHY: Verifies ToString handles null ReferenceSnippet.
    /// </summary>
    [Fact]
    public void ToString_WithNullReferenceSnippet_HandlesGracefully()
    {
        // Arrange
        var evidence = new Evidence { ReferenceSnippet = null };

        // Act
        var result = evidence.ToString();

        // Assert
        Assert.Contains("ReferenceSnippet:", result);
        // Should not throw
    }

    #endregion

    #region ToString - Formatting Tests

    /// <summary>
    /// WHY: Verifies ToString uses newlines between properties.
    /// </summary>
    [Fact]
    public void ToString_UsesNewlines()
    {
        // Arrange
        var evidence = new Evidence
        {
            EvidenceId = Guid.NewGuid(),
            Claim = "Test",
            Type = EvidenceType.VerifiableFact,
            Notes = "Notes",
            ReferenceSnippet = "Reference"
        };

        // Act
        var result = evidence.ToString();

        // Assert
        Assert.Contains("\n", result);
    }

    /// <summary>
    /// WHY: Verifies all properties appear in correct order.
    /// </summary>
    [Fact]
    public void ToString_PropertiesInExpectedOrder()
    {
        // Arrange
        var evidence = new Evidence
        {
            EvidenceId = Guid.NewGuid(),
            Claim = "Claim",
            Type = EvidenceType.VerifiableFact,
            Notes = "Notes",
            ReferenceSnippet = "Reference"
        };

        // Act
        var result = evidence.ToString();

        // Assert - Check order by index
        var evidenceIdIndex = result.IndexOf("EvidenceId:");
        var claimIndex = result.IndexOf("Claim:");
        var typeIndex = result.IndexOf("Type:");
        var notesIndex = result.IndexOf("Notes:");
        var referenceIndex = result.IndexOf("ReferenceSnippet:");

        Assert.True(evidenceIdIndex < claimIndex);
        Assert.True(claimIndex < typeIndex);
        Assert.True(typeIndex < notesIndex);
        Assert.True(notesIndex < referenceIndex);
    }

    #endregion

    #region Edge Cases

    /// <summary>
    /// WHY: Verifies handling of empty string properties.
    /// </summary>
    [Fact]
    public void ToString_WithEmptyStrings_HandlesGracefully()
    {
        // Arrange
        var evidence = new Evidence
        {
            Claim = "",
            Notes = "",
            ReferenceSnippet = ""
        };

        // Act
        var result = evidence.ToString();

        // Assert
        Assert.NotNull(result);
        Assert.Contains("Claim:", result);
    }

    /// <summary>
    /// WHY: Verifies handling of special characters.
    /// </summary>
    [Fact]
    public void ToString_WithSpecialCharacters_IncludesThem()
    {
        // Arrange
        var evidence = new Evidence
        {
            Claim = "Special \"chars\" & <tags>",
            Notes = "Line1\nLine2\tTab"
        };

        // Act
        var result = evidence.ToString();

        // Assert
        Assert.Contains("Special \"chars\" & <tags>", result);
        Assert.Contains("Line1\nLine2\tTab", result);
    }

    /// <summary>
    /// WHY: Verifies handling of very long strings.
    /// </summary>
    [Fact]
    public void ToString_WithLongStrings_IncludesFull()
    {
        // Arrange
        var longClaim = new string('x', 10000);
        var evidence = new Evidence { Claim = longClaim };

        // Act
        var result = evidence.ToString();

        // Assert
        Assert.Contains(longClaim, result);
    }

    /// <summary>
    /// WHY: Verifies ToString with all properties populated.
    /// </summary>
    [Fact]
    public void ToString_WithAllProperties_IncludesAll()
    {
        // Arrange
        var id = Guid.NewGuid();
        var evidence = new Evidence
        {
            EvidenceId = id,
            Claim = "Full claim text",
            Type = EvidenceType.VerifiableFact,
            Notes = "Detailed notes",
            ReferenceSnippet = "Source document reference"
        };

        // Act
        var result = evidence.ToString();

        // Assert
        Assert.Contains(id.ToString(), result);
        Assert.Contains("Full claim text", result);
        Assert.Contains("Fact", result);
        Assert.Contains("Detailed notes", result);
        Assert.Contains("Source document reference", result);
    }

    #endregion
}
