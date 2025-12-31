using NIU.ACH_AI.Application.DTOs;

namespace NIU.ACH_AI.Application.Interfaces
{
    public interface IAgentResponsePersistence
    {
        Task SaveAgentResponseAsync(
            AgentResponseRecord response,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Saves an agent response by extracting token usage from metadata and mapping properties internally.
        /// </summary>
        Task SaveAgentResponseAsync(
            string content,
            IReadOnlyDictionary<string, object?>? metadata,
            string agentName,
            Guid stepExecutionId,
            Guid agentConfigurationId,
            int turnNumber,
            long responseDuration,
            CancellationToken cancellationToken = default);
    }
}
