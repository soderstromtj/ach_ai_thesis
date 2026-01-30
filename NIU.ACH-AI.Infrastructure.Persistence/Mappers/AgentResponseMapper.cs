using NIU.ACH_AI.Application.DTOs;
using NIU.ACH_AI.Application.Interfaces;
using NIU.ACH_AI.Infrastructure.Persistence.Models;

namespace NIU.ACH_AI.Infrastructure.Persistence.Mappers
{
    /// <summary>
    /// Static mapper for converting between AgentResponse objects and records.
    /// </summary>
    public static class AgentResponseMapper
    {
        /// <summary>
        /// Maps an AgentResponseRecord to an AgentResponse entity.
        /// </summary>
        public static AgentResponse ToEntity(AgentResponseRecord record)
        {
            return new AgentResponse
            {
                AgentResponseId = Guid.NewGuid(),
                StepExecutionId = record.StepExecutionId,
                AgentConfigurationId = record.AgentConfigurationId,
                AgentName = record.AgentName,
                InputTokenCount = record.InputTokenCount,
                OutputTokenCount = record.OutputTokenCount,
                ContentLength = record.ContentLength,
                Content = record.Content,
                TurnNumber = record.TurnNumber,
                ResponseDuration = record.ResponseDuration,
                CreatedAt = record.CreatedAt,

                // Map extended metadata
                CompletionId = record.CompletionId,
                ReasoningTokenCount = record.ReasoningTokenCount,
                OutputAudioTokenCount = record.OutputAudioTokenCount,
                AcceptedPredictionTokenCount = record.AcceptedPredictionTokenCount,
                RejectedPredictionTokenCount = record.RejectedPredictionTokenCount,
                InputAudioTokenCount = record.InputAudioTokenCount,
                CachedInputTokenCount = record.CachedInputTokenCount,
                FinishedAt = record.FinishedAt
            };
        }

        /// <summary>
        /// Creates an AgentResponseRecord from raw inputs.
        /// </summary>
        public static AgentResponseRecord ToRecord(
            string content,
            string agentName,
            Guid stepExecutionId,
            Guid agentConfigurationId,
            int turnNumber,
            long responseDuration,
            ITokenUsageExtractor tokenUsageExtractor,
            IReadOnlyDictionary<string, object?>? metadata)
        {
            // Extract usage info
            var usageInfo = tokenUsageExtractor.ExtractTokenUsage(metadata);

            // Extract CompletionId from metadata if available
            string? completionId = null;
            if (metadata != null && metadata.TryGetValue("CompletionId", out var completionIdObj))
            {
                completionId = completionIdObj?.ToString();
            }

            return new AgentResponseRecord
            {
                StepExecutionId = stepExecutionId,
                AgentConfigurationId = agentConfigurationId,
                AgentName = agentName,
                Content = content,
                ContentLength = content?.Length ?? 0,
                ResponseDuration = responseDuration,
                TurnNumber = turnNumber,

                // Map from usage info
                InputTokenCount = usageInfo.InputTokenCount,
                OutputTokenCount = usageInfo.OutputTokenCount,
                ReasoningTokenCount = usageInfo.ReasoningTokenCount,
                OutputAudioTokenCount = usageInfo.OutputAudioTokenCount,
                AcceptedPredictionTokenCount = usageInfo.AcceptedPredictionTokenCount,
                RejectedPredictionTokenCount = usageInfo.RejectedPredictionTokenCount,
                InputAudioTokenCount = usageInfo.InputAudioTokenCount,
                CachedInputTokenCount = usageInfo.CachedInputTokenCount,
                CreatedAt = usageInfo.CreatedAt?.UtcDateTime ?? DateTime.UtcNow,

                CompletionId = completionId,
                FinishedAt = DateTime.UtcNow
            };
        }
    }
}
