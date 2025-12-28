using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.Agents.Orchestration;
using Microsoft.SemanticKernel.Agents.Orchestration.Concurrent;
using Microsoft.SemanticKernel.Agents.Orchestration.Transforms;
using NIU.ACH_AI.Application.Configuration;
using NIU.ACH_AI.Application.DTOs;
using NIU.ACH_AI.Application.Interfaces;
using NIU.ACH_AI.Domain.Entities;

namespace NIU.ACH_AI.Infrastructure.AI.Factories
{
#pragma warning disable SKEXP0110 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
    public class EvidenceExtractionOrchestrationFactory(
        IAgentService agentService,
        IKernelBuilderService kernelBuilderService,
        IOptions<OrchestrationSettings> orchestrationSettings,
        ILoggerFactory loggerFactory,
        IAgentResponsePersistence? agentResponsePersistence = null)
        : BaseOrchestrationFactory<List<Evidence>, EvidenceResult>(agentService, kernelBuilderService, orchestrationSettings, loggerFactory, agentResponsePersistence)
    {
        protected override ILogger CreateLogger(ILoggerFactory loggerFactory)
        {
            return loggerFactory.CreateLogger<EvidenceExtractionOrchestrationFactory>();
        }

        protected override AgentOrchestration<string, EvidenceResult> CreateOrchestration(
            OrchestrationPromptInput input,
            List<string> agentNames,
            Kernel kernel,
            Agent[] agents,
            StructuredOutputTransform<EvidenceResult> outputTransform)
        {
            // Create the ConcurrentOrchestration instance
            var orchestration = new ConcurrentOrchestration<string, EvidenceResult>(agents)
            {
                ResponseCallback = ResponseCallback,
                ResultTransform = outputTransform.TransformAsync,
                StreamingResponseCallback = StreamingResponseCallback,
            };

            return orchestration;
        }

        protected override string GetResultTypeName()
        {
            return nameof(EvidenceResult);
        }

        protected override List<Evidence> UnwrapResult(EvidenceResult wrapper)
        {
            return wrapper.Evidence;
        }

        protected override int GetItemCount(List<Evidence> result)
        {
            return result.Count;
        }

        protected override List<Evidence> CreateEmptyResult()
        {
            return new List<Evidence>();
        }

        protected override List<Evidence> CreateErrorResult()
        {
            return new List<Evidence>();
        }

        protected override string GetAgentSelectionReason(string? previousAgentName)
        {
            return "Concurrent execution - all agents run simultaneously";
        }
    }
}
#pragma warning restore SKEXP0110 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
