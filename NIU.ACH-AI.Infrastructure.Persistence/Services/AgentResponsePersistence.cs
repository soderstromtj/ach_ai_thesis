using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NIU.ACH_AI.Application.DTOs;
using NIU.ACH_AI.Application.Interfaces;
using NIU.ACH_AI.Infrastructure.Persistence.Mappers;
using NIU.ACH_AI.Infrastructure.Persistence.Models;

namespace NIU.ACH_AI.Infrastructure.Persistence.Services
{
    /// <summary>
    /// Persists agent response telemetry for a step execution.
    /// </summary>
    /// <summary>
    /// Persists agent response telemetry for a step execution.
    /// </summary>
    public class AgentResponsePersistence : IAgentResponsePersistence
    {
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly ITokenUsageExtractor _tokenUsageExtractor;

        /// <summary>
        /// Initializes a new instance of the <see cref="AgentResponsePersistence"/> class.
        /// </summary>
        /// <param name="serviceScopeFactory">Factory for creating service scopes to access scoped DbContext.</param>
        /// <param name="tokenUsageExtractor">Service to extract token usage from response metadata.</param>
        public AgentResponsePersistence(
            IServiceScopeFactory serviceScopeFactory,
            ITokenUsageExtractor tokenUsageExtractor)
        {
            _serviceScopeFactory = serviceScopeFactory ?? throw new ArgumentNullException(nameof(serviceScopeFactory));
            _tokenUsageExtractor = tokenUsageExtractor ?? throw new ArgumentNullException(nameof(tokenUsageExtractor));
        }

        /// <summary>
        /// Persists a fully populated agent response record to the database.
        /// </summary>
        /// <param name="response">The agent response record to save.</param>
        /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
        /// <exception cref="ArgumentNullException">Thrown when response is null.</exception>
        /// <exception cref="ArgumentException">Thrown when required identifiers in response are missing.</exception>
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

            // Use Mapper to create entity
            var entity = AgentResponseMapper.ToEntity(response);

            await SaveEntityAsync(entity, cancellationToken);
        }

        /// <summary>
        /// Persists an agent response by extracting metadata and constructing the record internally.
        /// </summary>
        /// <param name="content">The textual content of the response.</param>
        /// <param name="metadata">The metadata dictionary from the AI service response.</param>
        /// <param name="agentName">The name of the agent producing the response.</param>
        /// <param name="stepExecutionId">The ID of the current step execution.</param>
        /// <param name="agentConfigurationId">The ID of the agent's configuration.</param>
        /// <param name="turnNumber">The turn number in the conversation.</param>
        /// <param name="responseDuration">The duration of the response generation in milliseconds.</param>
        /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
        /// <exception cref="ArgumentException">Thrown when required arguments are invalid.</exception>
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

            // Use Mapper to create record
            var record = AgentResponseMapper.ToRecord(
                content,
                agentName,
                stepExecutionId,
                agentConfigurationId,
                turnNumber,
                responseDuration,
                _tokenUsageExtractor,
                metadata);

            await SaveAgentResponseAsync(record, cancellationToken);
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
