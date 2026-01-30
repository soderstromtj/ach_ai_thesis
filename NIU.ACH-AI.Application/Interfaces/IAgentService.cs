using Microsoft.SemanticKernel.Agents;

namespace NIU.ACH_AI.Application.Interfaces
{
    /// <summary>
    /// Defines the contract for creating instances of semantic kernel agents.
    /// </summary>
    public interface IAgentService
    {
        /// <summary>
        /// Creates the collection of agents configured for the service and optionally persists their configuration.
        /// </summary>
        /// <param name="stepExecutionId">Optional. If provided, persists agent configurations for this step execution.</param>
        /// <returns>A tuple containing the agents and a dictionary of their configuration IDs.</returns>
        (IEnumerable<Agent> Agents, Dictionary<string, Guid> ConfigurationIds) CreateAgents(Guid? stepExecutionId = null);
    }
}
