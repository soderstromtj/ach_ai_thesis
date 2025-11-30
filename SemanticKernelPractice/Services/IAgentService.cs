using Microsoft.SemanticKernel.Agents;

namespace SemanticKernelPractice.Services
{
    public interface IAgentService
    {
        IEnumerable<Agent> CreateAgents();
    }
}
