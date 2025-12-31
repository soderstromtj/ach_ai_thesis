using NIU.ACH_AI.Application.Configuration;

namespace NIU.ACH_AI.Application.Interfaces
{
    /// <summary>
    /// Defines the contract for persisting agent configurations used in an execution step.
    /// </summary>
    public interface IAgentConfigurationPersistence
    {
        /// <summary>
        /// Creates and persists agent configurations for a specific step execution.
        /// </summary>
        /// <param name="stepExecutionId">The ID of the step execution.</param>
        /// <param name="agentConfigurations">The collection of agent configurations to save.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A dictionary mapping agent names to their persisted unique identifiers.</returns>
        Task<IReadOnlyDictionary<string, Guid>> CreateAgentConfigurationsAsync(
            Guid stepExecutionId,
            IEnumerable<AgentConfiguration> agentConfigurations,
            CancellationToken cancellationToken = default);
    }
}
