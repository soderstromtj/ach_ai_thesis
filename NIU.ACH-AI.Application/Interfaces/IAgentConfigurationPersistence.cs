using NIU.ACH_AI.Application.Configuration;

namespace NIU.ACH_AI.Application.Interfaces
{
    public interface IAgentConfigurationPersistence
    {
        Task<IReadOnlyDictionary<string, Guid>> CreateAgentConfigurationsAsync(
            Guid stepExecutionId,
            IEnumerable<AgentConfiguration> agentConfigurations,
            CancellationToken cancellationToken = default);
    }
}
