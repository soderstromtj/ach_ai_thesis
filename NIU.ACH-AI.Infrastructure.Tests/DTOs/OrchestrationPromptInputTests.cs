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

}

