using Microsoft.SemanticKernel.Agents;

namespace NIU.ACH_AI.Application.Interfaces
{
    public interface IAgentService
    {
        IEnumerable<Agent> CreateAgents();
    }
}
