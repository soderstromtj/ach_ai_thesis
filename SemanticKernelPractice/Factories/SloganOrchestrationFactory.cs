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
    public class SloganOrchestrationFactory : IOrchestrationFactory<Analysis>
    {
        private readonly IAgentService _agentService;
        private readonly IKernelBuilderService _kernelBuilderService;
        private readonly OrchestrationSettings _orchestrationSettings;
        private readonly ChatHistory _history;

        public SloganOrchestrationFactory(
            IAgentService agentService,
            IKernelBuilderService kernelBuilderService,
            IOptions<OrchestrationSettings> orchestrationSettings)
        {
            _agentService = agentService;
            _kernelBuilderService = kernelBuilderService;
            _orchestrationSettings = orchestrationSettings.Value;
            _history = new ChatHistory();
        }

        async Task<Analysis> IOrchestrationFactory<Analysis>.ExecuteCoreAsync(string input, CancellationToken cancellationToken)
        {
            Agent[] agents = _agentService.CreateAgents().ToArray();

            if (agents.Count() < 3)
            {
                throw new InvalidOperationException("At least three agents are required for slogan orchestration.");
            }

            // Build kernel for output transformation
            Kernel kernel = _kernelBuilderService.BuildKernel();

            var outputTransform = new StructuredOutputTransform<Analysis>(
                kernel.GetRequiredService<IChatCompletionService>(),
                new OpenAIPromptExecutionSettings
                {
                    ResponseFormat = typeof(Analysis)
                });


            var manager = new SmartGroupChatManager
            {
                MaximumInvocationCount = _orchestrationSettings.MaximumInvocationCount,
            };


            GroupChatOrchestration<string, Analysis> orchestration = new GroupChatOrchestration<string, Analysis>(manager, agents)
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
                return new Analysis { 
                    ApprovedSlogan = "Error occurred",
                    Evaluation = ex.Message
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
            Console.WriteLine(response.Content);
            return ValueTask.CompletedTask;
        }

        public ChatHistory GetHistory() => _history;
    }
}
#pragma warning restore SKEXP0110 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.