using Microsoft.SemanticKernel.Agents;

namespace NIU.ACHAI.Application.Services
{
    public interface IAgentService
    {
        IEnumerable<Agent> CreateAgents();
    }
}
