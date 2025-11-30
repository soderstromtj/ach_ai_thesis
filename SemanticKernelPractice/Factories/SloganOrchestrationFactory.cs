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

namespace SemanticKernelPractice.Factories
{
#pragma warning disable SKEXP0110 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
    public class SloganOrchestrationFactory : IOrchestrationFactory<string>
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

        async Task<string> IOrchestrationFactory<string>.ExecuteCoreAsync(string input, CancellationToken cancellationToken)
        {
            Agent[] agents = _agentService.CreateAgents().ToArray();

            if (agents.Count() < 3)
            {
                throw new InvalidOperationException("At least three agents are required for slogan orchestration.");
            }

            // Build kernel for output transformation
            Kernel kernel = _kernelBuilderService.BuildKernel();

            var outputTransform = new StructuredOutputTransform<string>(
                kernel.GetRequiredService<IChatCompletionService>(),
                new OpenAIPromptExecutionSettings
                {
                    ResponseFormat = typeof(string)
                });


            var manager = new SmartGroupChatManager
            {
                MaximumInvocationCount = _orchestrationSettings.MaximumInvocationCount,
            };


            GroupChatOrchestration<string, string> orchestration = new GroupChatOrchestration<string, string>(manager, agents)
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
                return string.Empty;
            }
            finally
            {
                await runtime.RunUntilIdleAsync();
                Console.WriteLine("\nPress any key to exit...");
                Console.ReadKey();
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