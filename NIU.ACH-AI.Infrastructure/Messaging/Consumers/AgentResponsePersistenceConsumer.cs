using System.Threading.Tasks;
using MassTransit;
using Microsoft.Extensions.Logging;
using NIU.ACH_AI.Application.Messaging.Events;
using NIU.ACH_AI.Infrastructure.Persistence.Services;

namespace NIU.ACH_AI.Infrastructure.Messaging.Consumers
{
    /// <summary>
    /// Listens for new agent messages and saves them to the database.
    /// Decouples the message saving process from the main chat workflow.
    /// </summary>
    public class AgentResponsePersistenceConsumer : IConsumer<IAgentResponseReceived>
    {
        private readonly AgentResponsePersistence _persistenceService;
        private readonly ILogger<AgentResponsePersistenceConsumer> _logger;

        public AgentResponsePersistenceConsumer(
            AgentResponsePersistence persistenceService,
            ILogger<AgentResponsePersistenceConsumer> logger)
        {
            _persistenceService = persistenceService;
            _logger = logger;
        }

        public async Task Consume(ConsumeContext<IAgentResponseReceived> context)
        {
            var message = context.Message;
            _logger.LogInformation("Creating Agent Response Persistence for Agent {AgentName} (Turn {Turn})", message.AgentName, message.TurnNumber);

            try
            {
                // We use the concrete service directly (AgentResponsePersistence) to ensure we are calling the SQL implementation,
                // not the Messaging adapter (which would cause an infinite loop).
                await _persistenceService.SaveAgentResponseAsync(
                    message.Content,
                    message.Metadata,
                    message.AgentName,
                    message.StepExecutionId,
                    message.AgentConfigurationId,
                    message.TurnNumber,
                    message.ResponseDurationMs,
                    context.CancellationToken);

                _logger.LogInformation("Successfully persisted agent response for {AgentName}", message.AgentName);
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Failed to persist agent response for {AgentName}", message.AgentName);
                // In a real scenario, we might want to retry. MassTransit handles retries by default.
                throw; 
            }
        }
    }
}
