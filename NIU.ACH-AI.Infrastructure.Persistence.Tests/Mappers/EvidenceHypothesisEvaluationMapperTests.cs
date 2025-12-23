using FluentAssertions;
using NIU.ACH_AI.Domain.ValueObjects;
using NIU.ACH_AI.Infrastructure.Persistence.Mappers;
using DomainEntity = NIU.ACH_AI.Domain.Entities;
using DbModel = NIU.ACH_AI.Infrastructure.Persistence.Models;

namespace NIU.ACH_AI.Infrastructure.Persistence.Tests.Mappers;

/// <summary>
/// Unit tests for EvidenceHypothesisEvaluationMapper following FIRST principles.
/// Tests are Fast, Isolated, Repeatable, Self-validating, and Timely.
/// </summary>
public class EvidenceHypothesisEvaluationMapperTests
{
    #region ToDatabase Tests

    [Fact]
    public void ToDatabase_WithValidDomainEntity_GeneratesNewGuid()
    {
        // Arrange
        var domainEvaluation = new DomainEntity.EvidenceHypothesisEvaluation
        {
            Hypothesis = new DomainEntity.Hypothesis
            {
                ShortTitle = "Test Hypothesis",
                HypothesisText = "Hypothesis text"
            },
            Evidence = new DomainEntity.Evidence
            {
                EvidenceId = Guid.NewGuid(),
                Claim = "Test claim",
                Type = EvidenceType.Fact
            },
            Score = EvaluationScore.Consistent,
            ScoreRationale = "Test rationale",
            ConfidenceLevel = 0.85m,
            ConfidenceRationale = "High confidence"
        };
        var stepExecutionId = Guid.NewGuid();
        var hypothesisId = Guid.NewGuid();
        var evidenceId = Guid.NewGuid();

        // Act
        var result = EvidenceHypothesisEvaluationMapper.ToDatabase(
            domainEvaluation,
            stepExecutionId,
            hypothesisId,
            evidenceId);

        // Assert
        result.EvidenceHypothesisEvaluationId.Should().NotBe(Guid.Empty);
        result.StepExecutionId.Should().Be(stepExecutionId);
        result.HypothesisId.Should().Be(hypothesisId);
        result.EvidenceId.Should().Be(evidenceId);
        result.EvaluationScoreId.Should().Be((int)EvaluationScore.Consistent);
        result.Rationale.Should().Be("Test rationale");
        result.ConfidenceScore.Should().Be(0.85m);
        result.ConfidenceRationale.Should().Be("High confidence");
        result.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Theory]
    [InlineData(EvaluationScore.Consistent, 0)]
    [InlineData(EvaluationScore.Inconsistent, 1)]
    [InlineData(EvaluationScore.Neutral, 2)]
    public void ToDatabase_WithDifferentScores_MapsCorrectly(EvaluationScore score, int expectedId)
    {
        // Arrange
        var domainEvaluation = new DomainEntity.EvidenceHypothesisEvaluation
        {
            Hypothesis = new DomainEntity.Hypothesis(),
            Evidence = new DomainEntity.Evidence(),
            Score = score,
            ScoreRationale = "Test rationale",
            ConfidenceLevel = 0.5m,
            ConfidenceRationale = "Medium confidence"
        };
        var stepExecutionId = Guid.NewGuid();
        var hypothesisId = Guid.NewGuid();
        var evidenceId = Guid.NewGuid();

        // Act
        var result = EvidenceHypothesisEvaluationMapper.ToDatabase(
            domainEvaluation,
            stepExecutionId,
            hypothesisId,
            evidenceId);

        // Assert
        result.EvaluationScoreId.Should().Be(expectedId);
    }

    [Fact]
    public void ToDatabase_WithZeroConfidenceLevel_PreservesZero()
    {
        // Arrange
        var domainEvaluation = new DomainEntity.EvidenceHypothesisEvaluation
        {
            Hypothesis = new DomainEntity.Hypothesis(),
            Evidence = new DomainEntity.Evidence(),
            Score = EvaluationScore.Neutral,
            ScoreRationale = "Test rationale",
            ConfidenceLevel = 0m,
            ConfidenceRationale = "No confidence"
        };
        var stepExecutionId = Guid.NewGuid();
        var hypothesisId = Guid.NewGuid();
        var evidenceId = Guid.NewGuid();

        // Act
        var result = EvidenceHypothesisEvaluationMapper.ToDatabase(
            domainEvaluation,
            stepExecutionId,
            hypothesisId,
            evidenceId);

        // Assert
        result.ConfidenceScore.Should().Be(0m);
    }

    [Fact]
    public void ToDatabase_WithMaxConfidenceLevel_PreservesMaxValue()
    {
        // Arrange
        var domainEvaluation = new DomainEntity.EvidenceHypothesisEvaluation
        {
            Hypothesis = new DomainEntity.Hypothesis(),
            Evidence = new DomainEntity.Evidence(),
            Score = EvaluationScore.Consistent,
            ScoreRationale = "Test rationale",
            ConfidenceLevel = 1.0m,
            ConfidenceRationale = "Maximum confidence"
        };
        var stepExecutionId = Guid.NewGuid();
        var hypothesisId = Guid.NewGuid();
        var evidenceId = Guid.NewGuid();

        // Act
        var result = EvidenceHypothesisEvaluationMapper.ToDatabase(
            domainEvaluation,
            stepExecutionId,
            hypothesisId,
            evidenceId);

        // Assert
        result.ConfidenceScore.Should().Be(1.0m);
    }

    [Fact]
    public void ToDatabase_WithPreciseDecimalValue_PreservesPrecision()
    {
        // Arrange
        var domainEvaluation = new DomainEntity.EvidenceHypothesisEvaluation
        {
            Hypothesis = new DomainEntity.Hypothesis(),
            Evidence = new DomainEntity.Evidence(),
            Score = EvaluationScore.Consistent,
            ScoreRationale = "Test rationale",
            ConfidenceLevel = 0.123456789m,
            ConfidenceRationale = "Precise confidence"
        };
        var stepExecutionId = Guid.NewGuid();
        var hypothesisId = Guid.NewGuid();
        var evidenceId = Guid.NewGuid();

        // Act
        var result = EvidenceHypothesisEvaluationMapper.ToDatabase(
            domainEvaluation,
            stepExecutionId,
            hypothesisId,
            evidenceId);

        // Assert
        result.ConfidenceScore.Should().Be(0.123456789m);
    }

    [Fact]
    public void ToDatabase_WithEmptyStrings_PreservesEmptyStrings()
    {
        // Arrange
        var domainEvaluation = new DomainEntity.EvidenceHypothesisEvaluation
        {
            Hypothesis = new DomainEntity.Hypothesis(),
            Evidence = new DomainEntity.Evidence(),
            Score = EvaluationScore.Neutral,
            ScoreRationale = "",
            ConfidenceLevel = 0.5m,
            ConfidenceRationale = ""
        };
        var stepExecutionId = Guid.NewGuid();
        var hypothesisId = Guid.NewGuid();
        var evidenceId = Guid.NewGuid();

        // Act
        var result = EvidenceHypothesisEvaluationMapper.ToDatabase(
            domainEvaluation,
            stepExecutionId,
            hypothesisId,
            evidenceId);

        // Assert
        result.Rationale.Should().Be("");
        result.ConfidenceRationale.Should().Be("");
    }

    [Fact]
    public void ToDatabase_WithLongRationales_PreservesFullContent()
    {
        // Arrange
        var longRationale = new string('A', 10000);
        var longConfidenceRationale = new string('B', 10000);
        
        var domainEvaluation = new DomainEntity.EvidenceHypothesisEvaluation
        {
            Hypothesis = new DomainEntity.Hypothesis(),
            Evidence = new DomainEntity.Evidence(),
            Score = EvaluationScore.Consistent,
            ScoreRationale = longRationale,
            ConfidenceLevel = 0.9m,
            ConfidenceRationale = longConfidenceRationale
        };
        var stepExecutionId = Guid.NewGuid();
        var hypothesisId = Guid.NewGuid();
        var evidenceId = Guid.NewGuid();

        // Act
        var result = EvidenceHypothesisEvaluationMapper.ToDatabase(
            domainEvaluation,
            stepExecutionId,
            hypothesisId,
            evidenceId);

        // Assert
        result.Rationale.Should().Be(longRationale);
        result.ConfidenceRationale.Should().Be(longConfidenceRationale);
    }

    #endregion

    #region ToDomain Tests

    [Fact]
    public void ToDomain_WithValidDatabaseEntity_MapsAllProperties()
    {
        // Arrange
        var dbEvaluation = new DbModel.EvidenceHypothesisEvaluation
        {
            EvidenceHypothesisEvaluationId = Guid.NewGuid(),
            StepExecutionId = Guid.NewGuid(),
            HypothesisId = Guid.NewGuid(),
            EvidenceId = Guid.NewGuid(),
            EvaluationScoreId = (int)EvaluationScore.Consistent,
            Rationale = "Test rationale",
            ConfidenceScore = 0.85m,
            ConfidenceRationale = "High confidence",
            CreatedAt = DateTime.UtcNow,
            Hypothesis = new DbModel.Hypothesis
            {
                HypothesisId = Guid.NewGuid(),
                StepExecutionId = Guid.NewGuid(),
                ShortTitle = "Test Hypothesis",
                HypothesisText = "Hypothesis text",
                IsRefined = false,
                CreatedAt = DateTime.UtcNow
            },
            Evidence = new DbModel.Evidence
            {
                EvidenceId = Guid.NewGuid(),
                StepExecutionId = Guid.NewGuid(),
                Claim = "Test claim",
                ReferenceSnippet = "Test snippet",
                EvidenceTypeId = (int)EvidenceType.Fact,
                Notes = "Test notes",
                CreatedAt = DateTime.UtcNow
            }
        };

        // Act
        var result = EvidenceHypothesisEvaluationMapper.ToDomain(dbEvaluation);

        // Assert
        result.Hypothesis.Should().NotBeNull();
        result.Hypothesis.ShortTitle.Should().Be("Test Hypothesis");
        result.Evidence.Should().NotBeNull();
        result.Evidence.Claim.Should().Be("Test claim");
        result.Score.Should().Be(EvaluationScore.Consistent);
        result.ScoreRationale.Should().Be("Test rationale");
        result.ConfidenceLevel.Should().Be(0.85m);
        result.ConfidenceRationale.Should().Be("High confidence");
    }

    [Theory]
    [InlineData(0, EvaluationScore.Consistent)]
    [InlineData(1, EvaluationScore.Inconsistent)]
    [InlineData(2, EvaluationScore.Neutral)]
    public void ToDomain_WithDifferentScoreIds_MapsCorrectly(int scoreId, EvaluationScore expectedScore)
    {
        // Arrange
        var dbEvaluation = new DbModel.EvidenceHypothesisEvaluation
        {
            EvidenceHypothesisEvaluationId = Guid.NewGuid(),
            StepExecutionId = Guid.NewGuid(),
            HypothesisId = Guid.NewGuid(),
            EvidenceId = Guid.NewGuid(),
            EvaluationScoreId = scoreId,
            Rationale = "Test rationale",
            ConfidenceScore = 0.5m,
            ConfidenceRationale = "Medium confidence",
            CreatedAt = DateTime.UtcNow,
            Hypothesis = new DbModel.Hypothesis
            {
                HypothesisId = Guid.NewGuid(),
                StepExecutionId = Guid.NewGuid(),
                ShortTitle = "Test",
                HypothesisText = "Test",
                IsRefined = false,
                CreatedAt = DateTime.UtcNow
            },
            Evidence = new DbModel.Evidence
            {
                EvidenceId = Guid.NewGuid(),
                StepExecutionId = Guid.NewGuid(),
                Claim = "Test",
                EvidenceTypeId = 0,
                CreatedAt = DateTime.UtcNow
            }
        };

        // Act
        var result = EvidenceHypothesisEvaluationMapper.ToDomain(dbEvaluation);

        // Assert
        result.Score.Should().Be(expectedScore);
    }

    [Fact]
    public void ToDomain_WithNullRationale_ConvertsToEmptyString()
    {
        // Arrange
        var dbEvaluation = new DbModel.EvidenceHypothesisEvaluation
        {
            EvidenceHypothesisEvaluationId = Guid.NewGuid(),
            StepExecutionId = Guid.NewGuid(),
            HypothesisId = Guid.NewGuid(),
            EvidenceId = Guid.NewGuid(),
            EvaluationScoreId = (int)EvaluationScore.Neutral,
            Rationale = null,
            ConfidenceScore = 0.5m,
            ConfidenceRationale = "Medium confidence",
            CreatedAt = DateTime.UtcNow,
            Hypothesis = new DbModel.Hypothesis
            {
                HypothesisId = Guid.NewGuid(),
                StepExecutionId = Guid.NewGuid(),
                ShortTitle = "Test",
                HypothesisText = "Test",
                IsRefined = false,
                CreatedAt = DateTime.UtcNow
            },
            Evidence = new DbModel.Evidence
            {
                EvidenceId = Guid.NewGuid(),
                StepExecutionId = Guid.NewGuid(),
                Claim = "Test",
                EvidenceTypeId = 0,
                CreatedAt = DateTime.UtcNow
            }
        };

        // Act
        var result = EvidenceHypothesisEvaluationMapper.ToDomain(dbEvaluation);

        // Assert
        result.ScoreRationale.Should().Be(string.Empty);
    }

    [Fact]
    public void ToDomain_WithNullConfidenceScore_ConvertsToZero()
    {
        // Arrange
        var dbEvaluation = new DbModel.EvidenceHypothesisEvaluation
        {
            EvidenceHypothesisEvaluationId = Guid.NewGuid(),
            StepExecutionId = Guid.NewGuid(),
            HypothesisId = Guid.NewGuid(),
            EvidenceId = Guid.NewGuid(),
            EvaluationScoreId = (int)EvaluationScore.Neutral,
            Rationale = "Test rationale",
            ConfidenceScore = null,
            ConfidenceRationale = "Some rationale",
            CreatedAt = DateTime.UtcNow,
            Hypothesis = new DbModel.Hypothesis
            {
                HypothesisId = Guid.NewGuid(),
                StepExecutionId = Guid.NewGuid(),
                ShortTitle = "Test",
                HypothesisText = "Test",
                IsRefined = false,
                CreatedAt = DateTime.UtcNow
            },
            Evidence = new DbModel.Evidence
            {
                EvidenceId = Guid.NewGuid(),
                StepExecutionId = Guid.NewGuid(),
                Claim = "Test",
                EvidenceTypeId = 0,
                CreatedAt = DateTime.UtcNow
            }
        };

        // Act
        var result = EvidenceHypothesisEvaluationMapper.ToDomain(dbEvaluation);

        // Assert
        result.ConfidenceLevel.Should().Be(0);
    }

    [Fact]
    public void ToDomain_WithNullConfidenceRationale_ConvertsToEmptyString()
    {
        // Arrange
        var dbEvaluation = new DbModel.EvidenceHypothesisEvaluation
        {
            EvidenceHypothesisEvaluationId = Guid.NewGuid(),
            StepExecutionId = Guid.NewGuid(),
            HypothesisId = Guid.NewGuid(),
            EvidenceId = Guid.NewGuid(),
            EvaluationScoreId = (int)EvaluationScore.Neutral,
            Rationale = "Test rationale",
            ConfidenceScore = 0.5m,
            ConfidenceRationale = null,
            CreatedAt = DateTime.UtcNow,
            Hypothesis = new DbModel.Hypothesis
            {
                HypothesisId = Guid.NewGuid(),
                StepExecutionId = Guid.NewGuid(),
                ShortTitle = "Test",
                HypothesisText = "Test",
                IsRefined = false,
                CreatedAt = DateTime.UtcNow
            },
            Evidence = new DbModel.Evidence
            {
                EvidenceId = Guid.NewGuid(),
                StepExecutionId = Guid.NewGuid(),
                Claim = "Test",
                EvidenceTypeId = 0,
                CreatedAt = DateTime.UtcNow
            }
        };

        // Act
        var result = EvidenceHypothesisEvaluationMapper.ToDomain(dbEvaluation);

        // Assert
        result.ConfidenceRationale.Should().Be(string.Empty);
    }

    [Fact]
    public void ToDomain_WithAllNullableFieldsNull_HandlesGracefully()
    {
        // Arrange
        var dbEvaluation = new DbModel.EvidenceHypothesisEvaluation
        {
            EvidenceHypothesisEvaluationId = Guid.NewGuid(),
            StepExecutionId = Guid.NewGuid(),
            HypothesisId = Guid.NewGuid(),
            EvidenceId = Guid.NewGuid(),
            EvaluationScoreId = (int)EvaluationScore.Neutral,
            Rationale = null,
            ConfidenceScore = null,
            ConfidenceRationale = null,
            CreatedAt = DateTime.UtcNow,
            Hypothesis = new DbModel.Hypothesis
            {
                HypothesisId = Guid.NewGuid(),
                StepExecutionId = Guid.NewGuid(),
                ShortTitle = "Test",
                HypothesisText = "Test",
                IsRefined = false,
                CreatedAt = DateTime.UtcNow
            },
            Evidence = new DbModel.Evidence
            {
                EvidenceId = Guid.NewGuid(),
                StepExecutionId = Guid.NewGuid(),
                Claim = "Test",
                EvidenceTypeId = 0,
                CreatedAt = DateTime.UtcNow
            }
        };

        // Act
        var result = EvidenceHypothesisEvaluationMapper.ToDomain(dbEvaluation);

        // Assert
        result.ScoreRationale.Should().Be(string.Empty);
        result.ConfidenceLevel.Should().Be(0);
        result.ConfidenceRationale.Should().Be(string.Empty);
    }

    #endregion

    #region ToDomain Collection Tests

    [Fact]
    public void ToDomain_WithEmptyCollection_ReturnsEmptyList()
    {
        // Arrange
        var dbEntities = new List<DbModel.EvidenceHypothesisEvaluation>();

        // Act
        var result = EvidenceHypothesisEvaluationMapper.ToDomain(dbEntities);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public void ToDomain_WithMultipleEntities_MapsAllCorrectly()
    {
        // Arrange
        var dbEntities = new List<DbModel.EvidenceHypothesisEvaluation>
        {
            new DbModel.EvidenceHypothesisEvaluation
            {
                EvidenceHypothesisEvaluationId = Guid.NewGuid(),
                StepExecutionId = Guid.NewGuid(),
                HypothesisId = Guid.NewGuid(),
                EvidenceId = Guid.NewGuid(),
                EvaluationScoreId = (int)EvaluationScore.Consistent,
                Rationale = "Rationale 1",
                ConfidenceScore = 0.8m,
                ConfidenceRationale = "Confidence 1",
                CreatedAt = DateTime.UtcNow,
                Hypothesis = new DbModel.Hypothesis
                {
                    HypothesisId = Guid.NewGuid(),
                    StepExecutionId = Guid.NewGuid(),
                    ShortTitle = "Hypothesis 1",
                    HypothesisText = "Text 1",
                    IsRefined = false,
                    CreatedAt = DateTime.UtcNow
                },
                Evidence = new DbModel.Evidence
                {
                    EvidenceId = Guid.NewGuid(),
                    StepExecutionId = Guid.NewGuid(),
                    Claim = "Claim 1",
                    EvidenceTypeId = 0,
                    CreatedAt = DateTime.UtcNow
                }
            },
            new DbModel.EvidenceHypothesisEvaluation
            {
                EvidenceHypothesisEvaluationId = Guid.NewGuid(),
                StepExecutionId = Guid.NewGuid(),
                HypothesisId = Guid.NewGuid(),
                EvidenceId = Guid.NewGuid(),
                EvaluationScoreId = (int)EvaluationScore.Inconsistent,
                Rationale = "Rationale 2",
                ConfidenceScore = 0.6m,
                ConfidenceRationale = "Confidence 2",
                CreatedAt = DateTime.UtcNow,
                Hypothesis = new DbModel.Hypothesis
                {
                    HypothesisId = Guid.NewGuid(),
                    StepExecutionId = Guid.NewGuid(),
                    ShortTitle = "Hypothesis 2",
                    HypothesisText = "Text 2",
                    IsRefined = false,
                    CreatedAt = DateTime.UtcNow
                },
                Evidence = new DbModel.Evidence
                {
                    EvidenceId = Guid.NewGuid(),
                    StepExecutionId = Guid.NewGuid(),
                    Claim = "Claim 2",
                    EvidenceTypeId = 1,
                    CreatedAt = DateTime.UtcNow
                }
            }
        };

        // Act
        var result = EvidenceHypothesisEvaluationMapper.ToDomain(dbEntities);

        // Assert
        result.Should().HaveCount(2);
        result[0].Score.Should().Be(EvaluationScore.Consistent);
        result[0].Hypothesis.ShortTitle.Should().Be("Hypothesis 1");
        result[1].Score.Should().Be(EvaluationScore.Inconsistent);
        result[1].Hypothesis.ShortTitle.Should().Be("Hypothesis 2");
    }

    #endregion
}
