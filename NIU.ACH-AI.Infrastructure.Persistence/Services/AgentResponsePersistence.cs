using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NIU.ACH_AI.Application.DTOs;
using NIU.ACH_AI.Application.Interfaces;
using NIU.ACH_AI.Infrastructure.Persistence.Models;

namespace NIU.ACH_AI.Infrastructure.Persistence.Services
{
    /// <summary>
    /// Persists agent response telemetry for a step execution.
    /// </summary>
    public class AgentResponsePersistence : IAgentResponsePersistence
    {
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly ITokenUsageExtractor _tokenUsageExtractor;

        public AgentResponsePersistence(
            IServiceScopeFactory serviceScopeFactory,
            ITokenUsageExtractor tokenUsageExtractor)
        {
            _serviceScopeFactory = serviceScopeFactory ?? throw new ArgumentNullException(nameof(serviceScopeFactory));
            _tokenUsageExtractor = tokenUsageExtractor ?? throw new ArgumentNullException(nameof(tokenUsageExtractor));
        }

        public async Task SaveAgentResponseAsync(
            AgentResponseRecord response,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(response, nameof(response));

            if (response.StepExecutionId == Guid.Empty)
            {
                throw new ArgumentException("Step execution ID must be provided.", nameof(response));
            }

            if (response.AgentConfigurationId == Guid.Empty)
            {
                throw new ArgumentException("Agent configuration ID must be provided.", nameof(response));
            }

            if (string.IsNullOrWhiteSpace(response.AgentName))
            {
                throw new ArgumentException("Agent name must be provided.", nameof(response));
            }

            var entity = new AgentResponse
            {
                AgentResponseId = Guid.NewGuid(),
                StepExecutionId = response.StepExecutionId,
                AgentConfigurationId = response.AgentConfigurationId,
                AgentName = response.AgentName,
                InputTokenCount = response.InputTokenCount,
                OutputTokenCount = response.OutputTokenCount,
                ContentLength = response.ContentLength,
                Content = response.Content,
                TurnNumber = response.TurnNumber,
                ResponseDuration = response.ResponseDuration,
                CreatedAt = response.CreatedAt,

                // Map extended metadata
                CompletionId = response.CompletionId,
                ReasoningTokenCount = response.ReasoningTokenCount,
                OutputAudioTokenCount = response.OutputAudioTokenCount,
                AcceptedPredictionTokenCount = response.AcceptedPredictionTokenCount,
                RejectedPredictionTokenCount = response.RejectedPredictionTokenCount,
                InputAudioTokenCount = response.InputAudioTokenCount,
                CachedInputTokenCount = response.CachedInputTokenCount,
                FinishedAt = response.FinishedAt
            };

            await SaveEntityAsync(entity, cancellationToken);
        }

        public async Task SaveAgentResponseAsync(
            string content,
            IReadOnlyDictionary<string, object?>? metadata,
            string agentName,
            Guid stepExecutionId,
            Guid agentConfigurationId,
            int turnNumber,
            long responseDuration,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(agentName)) throw new ArgumentException("Agent name cannot be empty", nameof(agentName));
            if (stepExecutionId == Guid.Empty) throw new ArgumentException("StepExecutionId cannot be empty", nameof(stepExecutionId));
            if (agentConfigurationId == Guid.Empty) throw new ArgumentException("AgentConfigurationId cannot be empty", nameof(agentConfigurationId));

            // Extract usage info
            var usageInfo = _tokenUsageExtractor.ExtractTokenUsage(metadata);
            
            // Extract CompletionId from metadata if available
            string? completionId = null;
            if (metadata != null && metadata.TryGetValue("CompletionId", out var completionIdObj))
            {
                completionId = completionIdObj?.ToString();
            }

            var entity = new AgentResponse
            {
                AgentResponseId = Guid.NewGuid(),
                StepExecutionId = stepExecutionId,
                AgentConfigurationId = agentConfigurationId,
                AgentName = agentName,
                Content = content,
                ContentLength = content?.Length ?? 0,
                ResponseDuration = responseDuration,
                
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
                FinishedAt = DateTime.UtcNow,
                TurnNumber = turnNumber
            };

            await SaveEntityAsync(entity, cancellationToken);
        }

        private async Task SaveEntityAsync(AgentResponse entity, CancellationToken cancellationToken)
        {
            try
            {
                using (var scope = _serviceScopeFactory.CreateScope())
                {
                    var context = scope.ServiceProvider.GetRequiredService<AchAIDbContext>();
                    context.AgentResponses.Add(entity);
                    await context.SaveChangesAsync(cancellationToken);
                }
            }
            catch (DbUpdateException ex)
            {
                throw new InvalidOperationException("Failed to persist agent response.", ex);
            }
        }
    }
}
