using Azure.AI.Agents.Persistent;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.Agents.Orchestration;
using Microsoft.SemanticKernel.Agents.Orchestration.GroupChat;
using Microsoft.SemanticKernel.Agents.Orchestration.Transforms;
using Microsoft.SemanticKernel.Agents.Runtime.InProcess;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using SemanticKernelPractice.Configuration;
using SemanticKernelPractice.Managers;
using SemanticKernelPractice.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using SemanticKernelPractice.Models;

namespace SemanticKernelPractice.Factories
{
#pragma warning disable SKEXP0110 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
    public class EvidenceExtractionOrchestrationFactory : IOrchestrationFactory<List<Evidence>>
    {
        private readonly IAgentService _agentService;
        private readonly IKernelBuilderService _kernelBuilderService;
        private readonly OrchestrationSettings _orchestrationSettings;
        private readonly ChatHistory _history;

        public EvidenceExtractionOrchestrationFactory(
            IAgentService agentService,
            IKernelBuilderService kernelBuilderService,
            IOptions<OrchestrationSettings> orchestrationSettings)
        {
            _agentService = agentService;
            _kernelBuilderService = kernelBuilderService;
            _orchestrationSettings = orchestrationSettings.Value;
            _history = new ChatHistory();
        }

        async Task<List<Evidence>> IOrchestrationFactory<List<Evidence>>.ExecuteCoreAsync(string input, CancellationToken cancellationToken)
        {
            Agent[] agents = _agentService.CreateAgents().ToArray();

            if (agents.Count() < 3)
            {
                throw new InvalidOperationException("At least three agents are required for slogan orchestration.");
            }

            // Build kernel for output transformation
            Kernel kernel = _kernelBuilderService.BuildKernel();

            var outputTransform = new StructuredOutputTransform<List<Evidence>>(
                kernel.GetRequiredService<IChatCompletionService>(),
                new OpenAIPromptExecutionSettings
                {
                    ResponseFormat = typeof(List<Evidence>)
                });


            var manager = new SmartGroupChatManager
            {
                MaximumInvocationCount = _orchestrationSettings.MaximumInvocationCount,
            };


            GroupChatOrchestration<string, List<Evidence>> orchestration = new GroupChatOrchestration<string, List<Evidence>>(manager, agents)
            {
                ResponseCallback = ResponseCallback,
                ResultTransform = outputTransform.TransformAsync
            };

            var runtime = new InProcessRuntime();
            await runtime.StartAsync(cancellationToken);

            try
            {
                var result = await orchestration.InvokeAsync(input, runtime, cancellationToken);

                var output = await result.GetValueAsync(TimeSpan.FromMinutes(_orchestrationSettings.TimeoutInMinutes), cancellationToken);

                return output;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\nError: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
                return new List<Evidence>
                {
                    new Evidence
                    { 
                        Id = -1, 
                        Description = "Error during orchestration", 
                        Type = EvidenceType.Fact  
                    }
                };
            }
            finally
            {
                await runtime.RunUntilIdleAsync();
            }
        }

        private ValueTask ResponseCallback(ChatMessageContent response)
        {
            _history.Add(response);
            Console.WriteLine($"\n[{response.AuthorName}]");
            
            string contentToPrint = response.Content is not null
                ? (response.Content.Length > 100 ? response.Content.Substring(0, 100) : response.Content)
                : string.Empty;
            Console.WriteLine(contentToPrint);
            return ValueTask.CompletedTask;
        }

        public ChatHistory GetHistory() => _history;
    }
}
#pragma warning restore SKEXP0110 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.