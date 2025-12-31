using Microsoft.Extensions.Logging;
using NIU.ACH_AI.Application.DTOs;
using NIU.ACH_AI.Application.Interfaces;
using System;
using System.Collections.Generic;

namespace NIU.ACH_AI.Infrastructure.AI.Services
{
    public class TokenUsageExtractor : ITokenUsageExtractor
    {
        private readonly ILogger<TokenUsageExtractor> _logger;

        public TokenUsageExtractor(ILogger<TokenUsageExtractor> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public TokenUsageInfo ExtractTokenUsage(IReadOnlyDictionary<string, object?>? metadata)
        {
            var info = new TokenUsageInfo();
            if (metadata == null) return info;

            // Extract CreatedAt
            if (metadata.TryGetValue("CreatedAt", out var createdAtObj) && createdAtObj != null)
            {
                if (createdAtObj is DateTimeOffset dto) info.CreatedAt = dto;
                else if (createdAtObj is DateTime dt) info.CreatedAt = new DateTimeOffset(dt);
                else if (createdAtObj is string dateStr && DateTimeOffset.TryParse(dateStr, out var parsedDto)) info.CreatedAt = parsedDto;
            }

            if (metadata.TryGetValue("Usage", out var usageObj) && usageObj != null)
            {
                try
                {
                    // OpenAI.Chat.ChatTokenUsage via dynamic
                    dynamic dUsage = usageObj;
                   
                    // Extract top-level counts
                    try { info.OutputTokenCount = (int?)dUsage.OutputTokenCount; } catch { }
                    try { info.InputTokenCount = (int?)dUsage.InputTokenCount; } catch { }

                    dynamic? dOutputDetails = null;
                    dynamic? dInputDetails = null;

                    try { dOutputDetails = dUsage.OutputTokenDetails; } catch { }
                    try { dInputDetails = dUsage.InputTokenDetails; } catch { }

                    if (dOutputDetails != null)
                    {
                        try { info.ReasoningTokenCount = (int?)dOutputDetails.ReasoningTokenCount; } catch { }
                        try { info.OutputAudioTokenCount = (int?)dOutputDetails.AudioTokenCount; } catch { }
                        try { info.AcceptedPredictionTokenCount = (int?)dOutputDetails.AcceptedPredictionTokenCount; } catch { }
                        try { info.RejectedPredictionTokenCount = (int?)dOutputDetails.RejectedPredictionTokenCount; } catch { }
                    }

                    if (dInputDetails != null)
                    {
                        try { info.InputAudioTokenCount = (int?)dInputDetails.AudioTokenCount; } catch { }
                        try { info.CachedInputTokenCount = (int?)dInputDetails.CachedTokenCount; } catch { }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to extract metadata using dynamic/reflection strategy for type {Type}.", usageObj.GetType().FullName);
                }
            }

            // Fallback: Try top-level keys
            if (info.OutputTokenCount == null && metadata.TryGetValue("OutputTokenCount", out var outTokenObj) && outTokenObj is int outCount) info.OutputTokenCount = outCount;
            if (info.InputTokenCount == null && metadata.TryGetValue("InputTokenCount", out var inTokenObj) && inTokenObj is int inCount) info.InputTokenCount = inCount;

            return info;
        }
    }
}
