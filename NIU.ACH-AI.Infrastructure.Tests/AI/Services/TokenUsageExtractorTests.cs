using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using NIU.ACH_AI.Infrastructure.AI.Services;

namespace NIU.ACH_AI.Infrastructure.Tests.AI.Services
{
    public class TokenUsageExtractorTests
    {
        private readonly Mock<ILogger<TokenUsageExtractor>> _mockLogger;
        private readonly TokenUsageExtractor _extractor;

        public TokenUsageExtractorTests()
        {
            _mockLogger = new Mock<ILogger<TokenUsageExtractor>>();
            _extractor = new TokenUsageExtractor(_mockLogger.Object);
        }

        [Fact]
        public void ExtractTokenUsage_WithJsonElementMetadata_ExtractsCorrectly()
        {
            // Arrange
            // This raw JSON matches the user's provided sample
            var json = "{\"CompletionId\":\"chatcmpl-CyFvoAhDzoPrVKv0vABaX2lDltWLB\",\"CreatedAt\":\"2026-01-15T11:35:56+00:00\",\"SystemFingerprint\":null,\"RefusalUpdate\":null,\"Usage\":{\"outputTokenCount\":1940,\"inputTokenCount\":2096,\"totalTokenCount\":4036,\"outputTokenDetails\":{\"reasoningTokenCount\":832,\"audioTokenCount\":0,\"acceptedPredictionTokenCount\":0,\"rejectedPredictionTokenCount\":0},\"inputTokenDetails\":{\"audioTokenCount\":0,\"cachedTokenCount\":1792}},\"FinishReason\":null}";
            
            // Deserialize to Dictionary<string, object?> where the values will be JsonElements
            var metadata = JsonSerializer.Deserialize<Dictionary<string, object?>>(json);

            // Act
            var result = _extractor.ExtractTokenUsage(metadata);

            // Assert
            result.Should().NotBeNull();
            result.InputTokenCount.Should().Be(2096);
            result.OutputTokenCount.Should().Be(1940);
            result.ReasoningTokenCount.Should().Be(832);
            result.CachedInputTokenCount.Should().Be(1792);
        }

        [Fact]
        public void ExtractTokenUsage_WithNullMetadata_ReturnsEmpty()
        {
            // Act
            var result = _extractor.ExtractTokenUsage(null);

            // Assert
            result.InputTokenCount.Should().BeNull();
            result.OutputTokenCount.Should().BeNull();
        }
    }
}
