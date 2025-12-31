using Microsoft.SemanticKernel.Agents;

namespace NIU.ACH_AI.Application.Interfaces
{
    /// <summary>
    /// Defines the contract for creating instances of semantic kernel agents.
    /// </summary>
    public interface IAgentService
    {
        /// <summary>
        /// Creates the collection of agents configured for the service.
        /// </summary>
        /// <returns>An enumerable collection of <see cref="Agent"/> instances.</returns>
        IEnumerable<Agent> CreateAgents();
    }
}
