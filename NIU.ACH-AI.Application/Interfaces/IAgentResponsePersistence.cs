using NIU.ACH_AI.Application.DTOs;

namespace NIU.ACH_AI.Application.Interfaces
{
    public interface IAgentResponsePersistence
    {
        Task SaveAgentResponseAsync(
            AgentResponseRecord response,
            CancellationToken cancellationToken = default);
    }
}
