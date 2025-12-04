using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using SemanticKernelPractice.Configuration;

namespace SemanticKernelPractice.Services
{
    public class AgentService : IAgentService
    {
        private readonly IEnumerable<AgentConfiguration> _agentConfigurations;
        private readonly Kernel _kernel;

        public AgentService(
            IEnumerable<AgentConfiguration> agentConfigurations,
            Kernel kernel)
        {
            _agentConfigurations = agentConfigurations;
            _kernel = kernel;
        }

        // Need to update to include other types of agents based on the kernel type
        IEnumerable<Agent> IAgentService.CreateAgents()
        {
            return _agentConfigurations.Select(config =>
            {
                var agent = new ChatCompletionAgent
                {
                    Name = config.Name,
                    Description = config.Description,
                    Instructions = config.Instructions,
                    Kernel = _kernel
                };

                // If a specific service ID is configured for this agent, set it in Arguments
                if (!string.IsNullOrWhiteSpace(config.ServiceId))
                {
                    agent.Arguments = new KernelArguments(new PromptExecutionSettings
                    {
                        ServiceId = config.ServiceId
                    });
                }

                return agent;
            }).ToList();
        }
    }
}
