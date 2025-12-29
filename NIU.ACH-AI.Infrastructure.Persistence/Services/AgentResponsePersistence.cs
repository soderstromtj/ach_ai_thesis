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

        public AgentResponsePersistence(IServiceScopeFactory serviceScopeFactory)
        {
            _serviceScopeFactory = serviceScopeFactory ?? throw new ArgumentNullException(nameof(serviceScopeFactory));
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
                CreatedAt = response.CreatedAt
            };

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
