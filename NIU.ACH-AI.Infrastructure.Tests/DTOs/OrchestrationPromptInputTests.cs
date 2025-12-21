using NIU.ACH_AI.Application.DTOs;
using NIU.ACH_AI.Domain.Entities;

namespace NIU.ACH_AI.Infrastructure.Tests.DTOs;

/// <summary>
/// Unit tests for OrchestrationPromptInput.
///
/// Testing Strategy:
/// -----------------
/// OrchestrationPromptInput is a DTO with a complex ToString() method that
/// conditionally includes various properties.
///
/// Key testing areas:
/// 1. Property defaults - Empty strings, null optionals
/// 2. ToString - Conditional inclusion of properties
/// 3. JSON serialization of nested objects
/// </summary>
public class OrchestrationPromptInputTests
{
    #region Default Value Tests

    /// <summary>
    /// WHY: Verifies default instance has empty string properties.
    /// </summary>
    [Fact]
    public void NewInstance_HasEmptyStringDefaults()
    {
        // Arrange & Act
        var input = new OrchestrationPromptInput();

        // Assert
        Assert.Equal(string.Empty, input.KeyQuestion);
        Assert.Equal(string.Empty, input.Context);
        Assert.Equal(string.Empty, input.TaskInstructions);
        Assert.Equal(string.Empty, input.AdditionalInstructions);
    }

    /// <summary>
    /// WHY: Verifies default instance has null optional properties.
    /// </summary>
    [Fact]
    public void NewInstance_HasNullOptionalProperties()
    {
        // Arrange & Act
        var input = new OrchestrationPromptInput();

        // Assert
        Assert.Null(input.HypothesisResult);
        Assert.Null(input.EvidenceResult);
    }

    #endregion

    #region Property Assignment Tests

    /// <summary>
    /// WHY: Verifies properties can be assigned.
    /// </summary>
    [Fact]
    public void Properties_CanBeAssigned()
    {
        // Arrange
        var input = new OrchestrationPromptInput
        {
            KeyQuestion = "Test question",
            Context = "Test context",
            TaskInstructions = "Test instructions",
            AdditionalInstructions = "Additional info"
        };

        // Assert
        Assert.Equal("Test question", input.KeyQuestion);
        Assert.Equal("Test context", input.Context);
        Assert.Equal("Test instructions", input.TaskInstructions);
        Assert.Equal("Additional info", input.AdditionalInstructions);
    }

    /// <summary>
    /// WHY: Verifies HypothesisResult can be assigned.
    /// </summary>
    [Fact]
    public void HypothesisResult_CanBeAssigned()
    {
        // Arrange
        var hypothesisResult = new HypothesisResult
        {
            Hypotheses = new List<Hypothesis>
            {
                new Hypothesis { ShortTitle = "H1", HypothesisText = "Test" }
            }
        };

        var input = new OrchestrationPromptInput
        {
            HypothesisResult = hypothesisResult
        };

        // Assert
        Assert.NotNull(input.HypothesisResult);
        Assert.Single(input.HypothesisResult.Hypotheses);
    }

    /// <summary>
    /// WHY: Verifies EvidenceResult can be assigned.
    /// </summary>
    [Fact]
    public void EvidenceResult_CanBeAssigned()
    {
        // Arrange
        var evidenceResult = new EvidenceResult
        {
            Evidence = new List<Evidence>
            {
                new Evidence { Claim = "Test claim" }
            }
        };

        var input = new OrchestrationPromptInput
        {
            EvidenceResult = evidenceResult
        };

        // Assert
        Assert.NotNull(input.EvidenceResult);
        Assert.Single(input.EvidenceResult.Evidence);
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
        var input = new OrchestrationPromptInput();

        // Act
        var result = input.ToString();

        // Assert
        Assert.False(string.IsNullOrWhiteSpace(result));
    }

    /// <summary>
    /// WHY: Verifies ToString includes KeyQuestion.
    /// </summary>
    [Fact]
    public void ToString_IncludesKeyQuestion()
    {
        // Arrange
        var input = new OrchestrationPromptInput
        {
            KeyQuestion = "What is the cause?"
        };

        // Act
        var result = input.ToString();

        // Assert
        Assert.Contains("Key Question:", result);
        Assert.Contains("What is the cause?", result);
    }

    /// <summary>
    /// WHY: Verifies ToString includes Context.
    /// </summary>
    [Fact]
    public void ToString_IncludesContext()
    {
        // Arrange
        var input = new OrchestrationPromptInput
        {
            Context = "Background information"
        };

        // Act
        var result = input.ToString();

        // Assert
        Assert.Contains("Context:", result);
        Assert.Contains("Background information", result);
    }

    /// <summary>
    /// WHY: Verifies ToString includes TaskInstructions.
    /// </summary>
    [Fact]
    public void ToString_IncludesTaskInstructions()
    {
        // Arrange
        var input = new OrchestrationPromptInput
        {
            TaskInstructions = "Analyze the evidence"
        };

        // Act
        var result = input.ToString();

        // Assert
        Assert.Contains("Task Instructions:", result);
        Assert.Contains("Analyze the evidence", result);
    }

    #endregion

    #region ToString - Conditional Inclusion Tests

    /// <summary>
    /// WHY: Verifies ToString excludes AdditionalInstructions when empty.
    /// </summary>
    [Fact]
    public void ToString_WithEmptyAdditionalInstructions_ExcludesAdditionalInstructions()
    {
        // Arrange
        var input = new OrchestrationPromptInput
        {
            AdditionalInstructions = ""
        };

        // Act
        var result = input.ToString();

        // Assert
        Assert.DoesNotContain("Additional Instructions:", result);
    }

    /// <summary>
    /// WHY: Verifies ToString excludes AdditionalInstructions when whitespace.
    /// </summary>
    [Fact]
    public void ToString_WithWhitespaceAdditionalInstructions_ExcludesAdditionalInstructions()
    {
        // Arrange
        var input = new OrchestrationPromptInput
        {
            AdditionalInstructions = "   "
        };

        // Act
        var result = input.ToString();

        // Assert
        Assert.DoesNotContain("Additional Instructions:", result);
    }

    /// <summary>
    /// WHY: Verifies ToString includes AdditionalInstructions when set.
    /// </summary>
    [Fact]
    public void ToString_WithAdditionalInstructions_IncludesAdditionalInstructions()
    {
        // Arrange
        var input = new OrchestrationPromptInput
        {
            AdditionalInstructions = "Focus on recent events"
        };

        // Act
        var result = input.ToString();

        // Assert
        Assert.Contains("Additional Instructions:", result);
        Assert.Contains("Focus on recent events", result);
    }

    /// <summary>
    /// WHY: Verifies ToString excludes HypothesisResult when null.
    /// </summary>
    [Fact]
    public void ToString_WithNullHypothesisResult_ExcludesHypotheses()
    {
        // Arrange
        var input = new OrchestrationPromptInput
        {
            HypothesisResult = null
        };

        // Act
        var result = input.ToString();

        // Assert
        Assert.DoesNotContain("Hypotheses:", result);
    }

    /// <summary>
    /// WHY: Verifies ToString includes HypothesisResult when set.
    /// </summary>
    [Fact]
    public void ToString_WithHypothesisResult_IncludesHypotheses()
    {
        // Arrange
        var input = new OrchestrationPromptInput
        {
            HypothesisResult = new HypothesisResult
            {
                Hypotheses = new List<Hypothesis>
                {
                    new Hypothesis { ShortTitle = "H1" }
                }
            }
        };

        // Act
        var result = input.ToString();

        // Assert
        Assert.Contains("Hypotheses:", result);
    }

    /// <summary>
    /// WHY: Verifies ToString excludes EvidenceResult when null.
    /// </summary>
    [Fact]
    public void ToString_WithNullEvidenceResult_ExcludesEvidence()
    {
        // Arrange
        var input = new OrchestrationPromptInput
        {
            EvidenceResult = null
        };

        // Act
        var result = input.ToString();

        // Assert
        Assert.DoesNotContain("Evidence:", result);
    }

    /// <summary>
    /// WHY: Verifies ToString includes EvidenceResult when set.
    /// </summary>
    [Fact]
    public void ToString_WithEvidenceResult_IncludesEvidence()
    {
        // Arrange
        var input = new OrchestrationPromptInput
        {
            EvidenceResult = new EvidenceResult
            {
                Evidence = new List<Evidence>
                {
                    new Evidence { Claim = "Test claim" }
                }
            }
        };

        // Act
        var result = input.ToString();

        // Assert
        Assert.Contains("Evidence:", result);
    }

    #endregion

    #region ToString - JSON Serialization Tests

    /// <summary>
    /// WHY: Verifies HypothesisResult is serialized as JSON.
    /// </summary>
    [Fact]
    public void ToString_WithHypothesisResult_SerializesAsJson()
    {
        // Arrange
        var input = new OrchestrationPromptInput
        {
            HypothesisResult = new HypothesisResult
            {
                Hypotheses = new List<Hypothesis>
                {
                    new Hypothesis
                    {
                        ShortTitle = "TestHypothesis",
                        HypothesisText = "Test text"
                    }
                }
            }
        };

        // Act
        var result = input.ToString();

        // Assert - JSON should contain property names
        Assert.Contains("Hypotheses", result);
        Assert.Contains("ShortTitle", result);
    }

    /// <summary>
    /// WHY: Verifies EvidenceResult is serialized as JSON.
    /// </summary>
    [Fact]
    public void ToString_WithEvidenceResult_SerializesAsJson()
    {
        // Arrange
        var input = new OrchestrationPromptInput
        {
            EvidenceResult = new EvidenceResult
            {
                Evidence = new List<Evidence>
                {
                    new Evidence { Claim = "TestClaim" }
                }
            }
        };

        // Act
        var result = input.ToString();

        // Assert
        Assert.Contains("Evidence", result);
        Assert.Contains("Claim", result);
    }

    #endregion

    #region Edge Cases

    /// <summary>
    /// WHY: Verifies ToString handles special characters.
    /// </summary>
    [Fact]
    public void ToString_WithSpecialCharacters_HandlesGracefully()
    {
        // Arrange
        var input = new OrchestrationPromptInput
        {
            KeyQuestion = "What's the \"cause\" of <incident>?",
            Context = "Line1\nLine2\tTabbed"
        };

        // Act
        var result = input.ToString();

        // Assert
        Assert.Contains("What's the \"cause\" of <incident>?", result);
        Assert.Contains("Line1\nLine2\tTabbed", result);
    }

    /// <summary>
    /// WHY: Verifies ToString handles very long strings.
    /// </summary>
    [Fact]
    public void ToString_WithLongStrings_IncludesFullContent()
    {
        // Arrange
        var longString = new string('x', 10000);
        var input = new OrchestrationPromptInput
        {
            KeyQuestion = longString
        };

        // Act
        var result = input.ToString();

        // Assert
        Assert.Contains(longString, result);
    }

    /// <summary>
    /// WHY: Verifies ToString with all properties set.
    /// </summary>
    [Fact]
    public void ToString_WithAllPropertiesSet_IncludesAll()
    {
        // Arrange
        var input = new OrchestrationPromptInput
        {
            KeyQuestion = "Question",
            Context = "Context",
            TaskInstructions = "Instructions",
            AdditionalInstructions = "Additional",
            HypothesisResult = new HypothesisResult(),
            EvidenceResult = new EvidenceResult()
        };

        // Act
        var result = input.ToString();

        // Assert
        Assert.Contains("Key Question:", result);
        Assert.Contains("Context:", result);
        Assert.Contains("Task Instructions:", result);
        Assert.Contains("Additional Instructions:", result);
        Assert.Contains("Hypotheses:", result);
        Assert.Contains("Evidence:", result);
    }

    #endregion
}
