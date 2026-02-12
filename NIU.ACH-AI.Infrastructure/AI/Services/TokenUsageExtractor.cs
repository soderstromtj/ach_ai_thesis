using Microsoft.Extensions.Logging;
using NIU.ACH_AI.Application.DTOs;
using NIU.ACH_AI.Application.Interfaces;
using System;
using System.Collections.Generic;
using System.Text.Json;

namespace NIU.ACH_AI.Infrastructure.AI.Services
{
    /// <summary>
    /// Service for extracting token usage information from AI provider metadata.
    /// </summary>
    public class TokenUsageExtractor : ITokenUsageExtractor
    {
        private readonly ILogger<TokenUsageExtractor> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="TokenUsageExtractor"/> class.
        /// </summary>
        /// <param name="logger">The logger instance.</param>
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
                else if (createdAtObj is JsonElement je && je.ValueKind == JsonValueKind.String && DateTimeOffset.TryParse(je.GetString(), out var jeDto)) info.CreatedAt = jeDto;
            }

            if (metadata.TryGetValue("Usage", out var usageObj) && usageObj != null)
            {
                try
                {
                    if (usageObj is JsonElement usageElement)
                    {
                        info.OutputTokenCount = GetIntProperty(usageElement, "outputTokenCount", "OutputTokenCount");
                        info.InputTokenCount = GetIntProperty(usageElement, "inputTokenCount", "InputTokenCount");

                        if (usageElement.TryGetProperty("outputTokenDetails", out var outputDetails) || 
                            usageElement.TryGetProperty("OutputTokenDetails", out outputDetails))
                        {
                            info.ReasoningTokenCount = GetIntProperty(outputDetails, "reasoningTokenCount", "ReasoningTokenCount");
                            info.OutputAudioTokenCount = GetIntProperty(outputDetails, "audioTokenCount", "AudioTokenCount");
                            info.AcceptedPredictionTokenCount = GetIntProperty(outputDetails, "acceptedPredictionTokenCount", "AcceptedPredictionTokenCount");
                            info.RejectedPredictionTokenCount = GetIntProperty(outputDetails, "rejectedPredictionTokenCount", "RejectedPredictionTokenCount");
                        }

                        if (usageElement.TryGetProperty("inputTokenDetails", out var inputDetails) || 
                            usageElement.TryGetProperty("InputTokenDetails", out inputDetails))
                        {
                            info.InputAudioTokenCount = GetIntProperty(inputDetails, "audioTokenCount", "AudioTokenCount");
                            info.CachedInputTokenCount = GetIntProperty(inputDetails, "cachedTokenCount", "CachedTokenCount");
                        }
                    }
                    else
                    {
                        // Dynamic fallback for non-JsonElement types (reflection based for anonymous types or other objects)
                        dynamic dUsage = usageObj;
                        // Try dictionary access first (common for metadata dictionaries)
                        try { info.OutputTokenCount = ToInt(dUsage["outputTokenCount"]); } catch { }
                        if (info.OutputTokenCount == null) { try { info.OutputTokenCount = ToInt(dUsage["OutputTokenCount"]); } catch { } }

                        // Fallback to property access (for objects with properties)
                        if (info.OutputTokenCount == null) { try { info.OutputTokenCount = dUsage.OutputTokenCount; } catch { } }
                        if (info.OutputTokenCount == null) { try { info.OutputTokenCount = dUsage.outputTokenCount; } catch { } }

                        // Repeat for InputTokenCount
                        try { info.InputTokenCount = ToInt(dUsage["inputTokenCount"]); } catch { }
                        if (info.InputTokenCount == null) { try { info.InputTokenCount = ToInt(dUsage["InputTokenCount"]); } catch { } }
                        if (info.InputTokenCount == null) { try { info.InputTokenCount = dUsage.InputTokenCount; } catch { } }
                        if (info.InputTokenCount == null) { try { info.InputTokenCount = dUsage.inputTokenCount; } catch { } }

                        try { info.ReasoningTokenCount = ToInt(dUsage["reasoningTokenCount"]); } catch { }
                        if (info.ReasoningTokenCount == null) { try { info.ReasoningTokenCount = ToInt(dUsage["ReasoningTokenCount"]); } catch { } }
                        if (info.ReasoningTokenCount == null) { try { info.ReasoningTokenCount = dUsage.ReasoningTokenCount; } catch { } }
                        if (info.ReasoningTokenCount == null) { try { info.ReasoningTokenCount = dUsage.reasoningTokenCount; } catch { } }

                        try { info.OutputAudioTokenCount = ToInt(dUsage["audioTokenCount"]); } catch { }
                        if (info.OutputAudioTokenCount == null) { try { info.OutputAudioTokenCount = ToInt(dUsage["AudioTokenCount"]); } catch { } }
                        if (info.OutputAudioTokenCount == null) { try { info.OutputAudioTokenCount = dUsage.AudioTokenCount; } catch { } }
                        if (info.OutputAudioTokenCount == null) { try { info.OutputAudioTokenCount = dUsage.audioTokenCount; } catch { } }

                        try { info.AcceptedPredictionTokenCount = ToInt(dUsage["acceptedPredictionTokenCount"]); } catch { }
                        if (info.AcceptedPredictionTokenCount == null) { try { info.AcceptedPredictionTokenCount = ToInt(dUsage["AcceptedPredictionTokenCount"]); } catch { } }
                        if (info.AcceptedPredictionTokenCount == null) { try { info.AcceptedPredictionTokenCount = dUsage.AcceptedPredictionTokenCount; } catch { } }
                        if (info.AcceptedPredictionTokenCount == null) { try { info.AcceptedPredictionTokenCount = ToInt(dUsage.acceptedPredictionTokenCount); } catch { } }

                        try { info.RejectedPredictionTokenCount = ToInt(dUsage["rejectedPredictionTokenCount"]); } catch { }
                        if (info.RejectedPredictionTokenCount == null) { try { info.RejectedPredictionTokenCount = ToInt(dUsage["RejectedPredictionTokenCount"]); } catch { } }
                        if (info.RejectedPredictionTokenCount == null) { try { info.RejectedPredictionTokenCount = dUsage.RejectedPredictionTokenCount; } catch { } }
                        if (info.RejectedPredictionTokenCount == null) { try { info.RejectedPredictionTokenCount = ToInt(dUsage.rejectedPredictionTokenCount); } catch { } }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to extract metadata for type {Type}.", usageObj.GetType().FullName);
                }
            }

            return info;
        }

        private int? GetIntProperty(JsonElement element, params string[] propertyNames)
        {
            foreach (var name in propertyNames)
            {
                if (element.TryGetProperty(name, out var prop) && prop.ValueKind == JsonValueKind.Number && prop.TryGetInt32(out var val))
                {
                    return val;
                }
            }
            return null;
        }

        private int? ToInt(object? obj)
        {
            if (obj is int i) return i;
            if (obj is long l && l >= int.MinValue && l <= int.MaxValue) return (int)l;
            if (obj is JsonElement je && je.ValueKind == JsonValueKind.Number && je.TryGetInt32(out var val)) return val;
            return null;
        }
    }
}
