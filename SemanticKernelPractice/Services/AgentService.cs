using Azure.AI.Projects;
using Microsoft.SemanticKernel.Agents;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SemanticKernelPractice.Configuration;
using Microsoft.SemanticKernel;

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
            return _agentConfigurations.Select(config => new ChatCompletionAgent
            {
                Name = config.Name,
                Description = config.Description,
                Instructions = config.Instructions,
                Kernel = _kernel
            }).ToList();
        }
    }
}
