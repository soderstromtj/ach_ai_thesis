using NIU.ACH_AI.Application.DTOs;
using NIU.ACH_AI.Domain.Entities;
using NIU.ACH_AI.Domain.ValueObjects;

namespace NIU.ACH_AI.Infrastructure.Tests.DTOs;

/// <summary>
/// Unit tests for EvidenceHypothesisEvaluationResult.
///
/// Testing Strategy:
/// -----------------
/// EvidenceHypothesisEvaluationResult is a wrapper DTO with a complex ToString()
/// method that formats evaluations with hypothesis, evidence, scores, and rationales.
///
/// Key testing areas:
/// 1. Default values - Empty list
/// 2. ToString - Various evaluation scenarios
/// 3. Score formatting
/// 4. Rationale bullet points
/// </summary>
public class EvidenceHypothesisEvaluationResultTests
{
    #region Test Infrastructure

    private static EvidenceHypothesisEvaluation CreateEvaluation(
        string hypothesisTitle = "H1",
        string evidenceClaim = "E1",
        EvaluationScore score = EvaluationScore.Consistent,
        string rationale = "Test rationale")
    {
        return new EvidenceHypothesisEvaluation
        {
            Hypothesis = new Hypothesis
            {
                ShortTitle = hypothesisTitle,
                HypothesisText = $"Full text for {hypothesisTitle}"
            },
            Evidence = new Evidence
            {
                EvidenceId = Guid.NewGuid(),
                Claim = evidenceClaim,
                Type = EvidenceType.Fact
            },
            Score = score,
            ScoreRationale = rationale,
            ConfidenceLevel = 0.8m,
            ConfidenceRationale = "High confidence"
        };
    }

    #endregion

    #region Default Value Tests

    /// <summary>
    /// WHY: Verifies default instance has empty evaluations list.
    /// </summary>
    [Fact]
    public void NewInstance_HasEmptyEvaluationsList()
    {
        // Arrange & Act
        var result = new EvidenceHypothesisEvaluationResult();

        // Assert
        Assert.NotNull(result.Evaluations);
        Assert.Empty(result.Evaluations);
    }

    /// <summary>
    /// WHY: Verifies Evaluations property is not null by default.
    /// </summary>
    [Fact]
    public void Evaluations_IsNotNullByDefault()
    {
        // Arrange & Act
        var result = new EvidenceHypothesisEvaluationResult();

        // Assert
        Assert.NotNull(result.Evaluations);
    }

    #endregion

    #region Property Assignment Tests

    /// <summary>
    /// WHY: Verifies evaluations can be assigned.
    /// </summary>
    [Fact]
    public void Evaluations_CanBeAssigned()
    {
        // Arrange
        var evaluations = new List<EvidenceHypothesisEvaluation>
        {
            CreateEvaluation("H1"),
            CreateEvaluation("H2")
        };

        var result = new EvidenceHypothesisEvaluationResult
        {
            Evaluations = evaluations
        };

        // Assert
        Assert.Equal(2, result.Evaluations.Count);
    }

    #endregion

    #region ToString - Empty Evaluations Tests

    /// <summary>
    /// WHY: Verifies ToString handles empty evaluations list.
    /// </summary>
    [Fact]
    public void ToString_WithEmptyEvaluations_ReturnsValidString()
    {
        // Arrange
        var result = new EvidenceHypothesisEvaluationResult();

        // Act
        var output = result.ToString();

        // Assert
        Assert.NotNull(output);
        Assert.Contains("No evaluations available.", output);
    }

    #endregion

    #region ToString - Single Evaluation Tests

    /// <summary>
    /// WHY: Verifies ToString includes hypothesis from first evaluation.
    /// </summary>
    [Fact]
    public void ToString_WithSingleEvaluation_IncludesHypothesis()
    {
        // Arrange
        var result = new EvidenceHypothesisEvaluationResult
        {
            Evaluations = new List<EvidenceHypothesisEvaluation>
            {
                CreateEvaluation("TestHypothesis")
            }
        };

        // Act
        var output = result.ToString();

        // Assert
        Assert.Contains("Hypothesis:", output);
    }

    /// <summary>
    /// WHY: Verifies ToString includes evidence from first evaluation.
    /// </summary>
    [Fact]
    public void ToString_WithSingleEvaluation_IncludesEvidence()
    {
        // Arrange
        var result = new EvidenceHypothesisEvaluationResult
        {
            Evaluations = new List<EvidenceHypothesisEvaluation>
            {
                CreateEvaluation(evidenceClaim: "TestEvidence")
            }
        };

        // Act
        var output = result.ToString();

        // Assert
        Assert.Contains("Evidence:", output);
    }

    /// <summary>
    /// WHY: Verifies ToString includes evaluation score.
    /// </summary>
    [Fact]
    public void ToString_WithSingleEvaluation_IncludesScore()
    {
        // Arrange
        var result = new EvidenceHypothesisEvaluationResult
        {
            Evaluations = new List<EvidenceHypothesisEvaluation>
            {
                CreateEvaluation(score: EvaluationScore.Consistent)
            }
        };

        // Act
        var output = result.ToString();

        // Assert
        Assert.Contains("Consistent", output);
    }

    /// <summary>
    /// WHY: Verifies ToString includes rationale with bullet point.
    /// </summary>
    [Fact]
    public void ToString_WithSingleEvaluation_IncludesRationaleWithBullet()
    {
        // Arrange
        var result = new EvidenceHypothesisEvaluationResult
        {
            Evaluations = new List<EvidenceHypothesisEvaluation>
            {
                CreateEvaluation(rationale: "This is the rationale")
            }
        };

        // Act
        var output = result.ToString();

        // Assert
        Assert.Contains("Score Rationale: This is the rationale", output);
    }

    #endregion

    #region ToString - Multiple Evaluations Tests

    /// <summary>
    /// WHY: Verifies ToString lists all scores comma-separated.
    /// </summary>
    [Fact]
    public void ToString_WithMultipleEvaluations_ListsAllScores()
    {
        // Arrange
        var result = new EvidenceHypothesisEvaluationResult
        {
            Evaluations = new List<EvidenceHypothesisEvaluation>
            {
                CreateEvaluation(score: EvaluationScore.Consistent),
                CreateEvaluation(score: EvaluationScore.Inconsistent),
                CreateEvaluation(score: EvaluationScore.Neutral)
            }
        };

        // Act
        var output = result.ToString();

        // Assert
        Assert.Contains("Consistent", output);
        Assert.Contains("Inconsistent", output);
        Assert.Contains("Neutral", output);
    }

    /// <summary>
    /// WHY: Verifies ToString lists all rationales as bullets.
    /// </summary>
    [Fact]
    public void ToString_WithMultipleEvaluations_ListsAllRationales()
    {
        // Arrange
        var result = new EvidenceHypothesisEvaluationResult
        {
            Evaluations = new List<EvidenceHypothesisEvaluation>
            {
                CreateEvaluation(rationale: "First rationale"),
                CreateEvaluation(rationale: "Second rationale"),
                CreateEvaluation(rationale: "Third rationale")
            }
        };

        // Act
        var output = result.ToString();

        // Assert
        Assert.Contains("Score Rationale: First rationale", output);
        Assert.Contains("Score Rationale: Second rationale", output);
        Assert.Contains("Score Rationale: Third rationale", output);
    }
    #endregion

    #region ToString - Score Type Tests

    /// <summary>
    /// WHY: Verifies Consistent score is formatted correctly.
    /// </summary>
    [Fact]
    public void ToString_WithConsistentScore_FormatsCorrectly()
    {
        // Arrange
        var result = new EvidenceHypothesisEvaluationResult
        {
            Evaluations = new List<EvidenceHypothesisEvaluation>
            {
                CreateEvaluation(score: EvaluationScore.Consistent)
            }
        };

        // Act
        var output = result.ToString();

        // Assert
        Assert.Contains("Consistent", output);
    }

    /// <summary>
    /// WHY: Verifies Inconsistent score is formatted correctly.
    /// </summary>
    [Fact]
    public void ToString_WithInconsistentScore_FormatsCorrectly()
    {
        // Arrange
        var result = new EvidenceHypothesisEvaluationResult
        {
            Evaluations = new List<EvidenceHypothesisEvaluation>
            {
                CreateEvaluation(score: EvaluationScore.Inconsistent)
            }
        };

        // Act
        var output = result.ToString();

        // Assert
        Assert.Contains("Inconsistent", output);
    }

    /// <summary>
    /// WHY: Verifies Neutral score is formatted correctly.
    /// </summary>
    [Fact]
    public void ToString_WithNeutralScore_FormatsCorrectly()
    {
        // Arrange
        var result = new EvidenceHypothesisEvaluationResult
        {
            Evaluations = new List<EvidenceHypothesisEvaluation>
            {
                CreateEvaluation(score: EvaluationScore.Neutral)
            }
        };

        // Act
        var output = result.ToString();

        // Assert
        Assert.Contains("Neutral", output);
    }

    #endregion

    #region Edge Cases

    /// <summary>
    /// WHY: Verifies handling of empty rationale.
    /// </summary>
    [Fact]
    public void ToString_WithEmptyRationale_IncludesEmptyBullet()
    {
        // Arrange
        var result = new EvidenceHypothesisEvaluationResult
        {
            Evaluations = new List<EvidenceHypothesisEvaluation>
            {
                CreateEvaluation(rationale: "")
            }
        };

        // Act
        var output = result.ToString();

        // Assert
        Assert.Contains("Score Rationale:", output);
    }

    /// <summary>
    /// WHY: Verifies handling of many evaluations.
    /// </summary>
    [Fact]
    public void ToString_WithManyEvaluations_IncludesAll()
    {
        // Arrange
        var evaluations = Enumerable.Range(1, 20)
            .Select(i => CreateEvaluation(rationale: $"Rationale {i}"))
            .ToList();

        var result = new EvidenceHypothesisEvaluationResult
        {
            Evaluations = evaluations
        };

        // Act
        var output = result.ToString();

        // Assert
        for (int i = 1; i <= 20; i++)
        {
            Assert.Contains($"Score Rationale: Rationale {i}", output);
        }
    }

    #endregion
}
