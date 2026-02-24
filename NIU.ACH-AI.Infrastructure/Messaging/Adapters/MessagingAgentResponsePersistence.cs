using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MassTransit;
using Microsoft.Extensions.Logging;
using NIU.ACH_AI.Application.Interfaces;
using NIU.ACH_AI.Application.Messaging.Events;

namespace NIU.ACH_AI.Infrastructure.Messaging.Adapters
{
    /// <summary>
    /// Custom persistence adapter that publishes agent responses to the message broker.
    /// Used to decouple the database saving process from the live chat workflow.
    /// </summary>
    public class MessagingAgentResponsePersistence : IAgentResponsePersistence
    {
        private readonly IPublishEndpoint _publishEndpoint;
        private readonly ILogger<MessagingAgentResponsePersistence> _logger;

        public MessagingAgentResponsePersistence(
            IPublishEndpoint publishEndpoint,
            ILogger<MessagingAgentResponsePersistence> logger)
        {
            _publishEndpoint = publishEndpoint;
            _logger = logger;
        }

        public async Task SaveAgentResponseAsync(
            string content,
            IReadOnlyDictionary<string, object?>? metadata,
            string agentName,
            Guid stepExecutionId,
            Guid agentConfigurationId,
            int turnNumber,
            long responseDurationMs,
            CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Publishing agent response event for Agent {AgentName}, Step {StepExecutionId}", agentName, stepExecutionId);

            await _publishEndpoint.Publish<IAgentResponseReceived>(new
            {
                StepExecutionId = stepExecutionId,
                AgentConfigurationId = agentConfigurationId,
                AgentName = agentName,
                Content = content,
                Metadata = metadata,
                TurnNumber = turnNumber,
                ResponseDurationMs = responseDurationMs,
                Timestamp = DateTime.UtcNow
            }, cancellationToken);
        }

        public async Task SaveAgentResponseAsync(
            NIU.ACH_AI.Application.DTOs.AgentResponseRecord response,
            CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Publishing agent response record for Agent {AgentName}, Step {StepExecutionId}", response.AgentName, response.StepExecutionId);

            await _publishEndpoint.Publish<IAgentResponseReceived>(new
            {
                StepExecutionId = response.StepExecutionId,
                AgentConfigurationId = response.AgentConfigurationId,
                AgentName = response.AgentName,
                Content = response.Content,
                Metadata = (IReadOnlyDictionary<string, object?>?)null, // Record might not have raw metadata, or we need to look closer
                TurnNumber = response.TurnNumber,
                ResponseDurationMs = response.ResponseDuration,
                Timestamp = response.CreatedAt
            }, cancellationToken);
        }
    }
}
