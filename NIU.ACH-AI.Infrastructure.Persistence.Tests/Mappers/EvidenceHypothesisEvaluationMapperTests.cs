using FluentAssertions;
using NIU.ACH_AI.Domain.ValueObjects;
using NIU.ACH_AI.Infrastructure.Persistence.Mappers;
using DomainEntity = NIU.ACH_AI.Domain.Entities;
using DbModel = NIU.ACH_AI.Infrastructure.Persistence.Models;

namespace NIU.ACH_AI.Infrastructure.Persistence.Tests.Mappers;

/// <summary>
/// Unit tests for EvidenceHypothesisEvaluationMapper ensuring correct ID mapping.
/// </summary>
public class EvidenceHypothesisEvaluationMapperTests
{
    [Theory]
    [InlineData(EvaluationScore.VeryConsistent, 1)]
    [InlineData(EvaluationScore.Consistent, 2)]
    [InlineData(EvaluationScore.Neutral, 3)]
    [InlineData(EvaluationScore.Inconsistent, 4)]
    [InlineData(EvaluationScore.VeryInconsistent, 5)]
    public void ToDatabase_MapsEnumToCorrectId(EvaluationScore score, int expectedId)
    {
        // Arrange
        var domainInfo = new DomainEntity.EvidenceHypothesisEvaluation
        {
            Score = score,
            ScoreRationale = "Test rationale",
            ConfidenceLevel = 0.8m,
            ConfidenceRationale = "High confidence"
        };
        var stepExecutionId = Guid.NewGuid();
        var hypothesisId = Guid.NewGuid();
        var evidenceId = Guid.NewGuid();

        // Act
        var result = EvidenceHypothesisEvaluationMapper.ToDatabase(domainInfo, stepExecutionId, hypothesisId, evidenceId);

        // Assert
        result.EvaluationScoreId.Should().Be(expectedId);
    }

    [Theory]
    [InlineData(1, EvaluationScore.VeryConsistent)]
    [InlineData(2, EvaluationScore.Consistent)]
    [InlineData(3, EvaluationScore.Neutral)]
    [InlineData(4, EvaluationScore.Inconsistent)]
    [InlineData(5, EvaluationScore.VeryInconsistent)]
    public void ToDomain_MapsIdToCorrectEnum(int dbId, EvaluationScore expectedScore)
    {
        // Arrange
        var dbEntity = new DbModel.EvidenceHypothesisEvaluation
        {
            EvidenceHypothesisEvaluationId = Guid.NewGuid(),
            EvaluationScoreId = dbId,
            Rationale = "Test",
            ConfidenceScore = 0.5m,
            ConfidenceRationale = "Test",
            Hypothesis = new DbModel.Hypothesis { HypothesisText = "H1", ShortTitle = "H1" },
            Evidence = new DbModel.Evidence { Claim = "E1" }
        };

        // Act
        var result = EvidenceHypothesisEvaluationMapper.ToDomain(dbEntity);

        // Assert
        result.Score.Should().Be(expectedScore);
    }
}
