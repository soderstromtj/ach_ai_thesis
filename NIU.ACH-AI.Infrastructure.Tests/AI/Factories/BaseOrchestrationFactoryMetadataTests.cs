using System.Collections.Concurrent;
using System.Reflection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.Agents.Orchestration;
using Microsoft.SemanticKernel.Agents.Orchestration.Transforms;
using Microsoft.SemanticKernel.ChatCompletion;
using Moq;
using NIU.ACH_AI.Application.Configuration;
using NIU.ACH_AI.Application.DTOs;
using NIU.ACH_AI.Application.Interfaces;
using NIU.ACH_AI.Domain.Entities;
using NIU.ACH_AI.Infrastructure.AI.Factories;
using Xunit;

namespace NIU.ACH_AI.Infrastructure.Tests.AI.Factories
{
#pragma warning disable SKEXP0110
    public class BaseOrchestrationFactoryMetadataTests
    {
        private class TestableOrchestrationFactory : BaseOrchestrationFactory<List<Evidence>, EvidenceResult>
        {
            public TestableOrchestrationFactory(
                IAgentService agentService,
                IKernelBuilderService kernelBuilderService,
                IOptions<OrchestrationSettings> orchestrationSettings,
                ILoggerFactory loggerFactory,
                IAgentResponsePersistence? agentResponsePersistence = null)
                : base(agentService, kernelBuilderService, orchestrationSettings, loggerFactory, agentResponsePersistence)
            {
            }

            public ValueTask InvokeStreamingResponseCallback(StreamingChatMessageContent response, bool isFinal)
                => StreamingResponseCallback(response, isFinal);

            public ValueTask InvokeResponseCallback(ChatMessageContent response)
                => ResponseCallback(response);

            // Helper to set StepExecutionContext via reflection since it is private
            public void SetStepExecutionContext(StepExecutionContext context)
            {
                var field = typeof(BaseOrchestrationFactory<List<Evidence>, EvidenceResult>)
                    .GetField("_stepExecutionContext", BindingFlags.NonPublic | BindingFlags.Instance);
                field!.SetValue(this, context);
            }

            // Abstract implementations (not used for these tests but required)
            protected override ILogger CreateLogger(ILoggerFactory loggerFactory) => loggerFactory.CreateLogger("Test");
            protected override AgentOrchestration<string, EvidenceResult> CreateOrchestration(OrchestrationPromptInput input, List<string> agentNames, Kernel kernel, Agent[] agents, StructuredOutputTransform<EvidenceResult> outputTransform) => null!;
            protected override string GetResultTypeName() => "EvidenceResult";
            protected override List<Evidence> UnwrapResult(EvidenceResult wrapper) => new();
            protected override int GetItemCount(List<Evidence> result) => 0;
            protected override List<Evidence> CreateEmptyResult() => new();
            protected override List<Evidence> CreateErrorResult() => new();
            protected override string GetAgentSelectionReason(string? previousAgentName) => "Test";
        }

        [Fact]
        public async Task StreamingResponseCallback_WhenFinal_PersistsDirectlyWithMetadata()
        {
            // Arrange
            var agentName = "TestAgent";
            var persistenceMock = new Mock<IAgentResponsePersistence>();

            // Create factory with StreamResponses = true
            var factory = CreateFactory(persistenceMock.Object, streamResponses: true);

            // Set up context
            var executionId = Guid.NewGuid();
            var configId = Guid.NewGuid();
            var context = new StepExecutionContext
            {
                StepExecutionId = executionId,
                AgentConfigurationIds = new Dictionary<string, Guid> { { agentName, configId } }
            };
            factory.SetStepExecutionContext(context);

            // Create metadata mimicking the structure we expect (using anonymous objects for dynamic access)
            var metadata = new Dictionary<string, object>
            {
                { "CompletionId", "cmpl-direct-persist" },
                { "Usage", new {
                        OutputTokenDetails = new { ReasoningTokenCount = 123, AudioTokenCount = 5 },
                        InputTokenDetails = new { AudioTokenCount = 10, CachedTokenCount = 50 }
                    }
                }
            };

            // 1. Send a content chunk
            var chunkResponse = new StreamingChatMessageContent(AuthorRole.Assistant, "Part 1 ")
            {
                AuthorName = agentName
            };
            await factory.InvokeStreamingResponseCallback(chunkResponse, isFinal: false);

            // 2. Send final chunk with metadata
            var finalResponse = new StreamingChatMessageContent(AuthorRole.Assistant, "Part 2")
            {
                AuthorName = agentName,
                Metadata = metadata
            };

            // Act
            await factory.InvokeStreamingResponseCallback(finalResponse, isFinal: true);

            // Assert
            // Verify persistence was called with the aggregated content and correct metadata
            persistenceMock.Verify(p => p.SaveAgentResponseAsync(
                It.Is<AgentResponseRecord>(r =>
                    r.AgentName == agentName &&
                    r.Content == "Part 1 Part 2" && // Aggregated content
                    r.CompletionId == "cmpl-direct-persist" &&
                    r.ReasoningTokenCount == 123 &&
                    r.OutputAudioTokenCount == 5 &&
                    r.InputAudioTokenCount == 10 &&
                    r.CachedInputTokenCount == 50),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task ResponseCallback_WhenStreamingEnabled_DoesNotPersistAgain()
        {
            // Arrange
            var agentName = "TestAgent";
            var persistenceMock = new Mock<IAgentResponsePersistence>();

            // Create factory with StreamResponses = true
            var factory = CreateFactory(persistenceMock.Object, streamResponses: true);

            factory.SetStepExecutionContext(new StepExecutionContext
            {
                StepExecutionId = Guid.NewGuid(),
                AgentConfigurationIds = new Dictionary<string, Guid> { { agentName, Guid.NewGuid() } }
            });

            // Act
            // Call ResponseCallback (which happens after streaming)
            await factory.InvokeResponseCallback(new ChatMessageContent(AuthorRole.Assistant, "Final Content")
            {
                AuthorName = agentName
            });

            // Assert
            // Persistence should NOT be called here because it was handled in streaming
            persistenceMock.Verify(p => p.SaveAgentResponseAsync(
                It.IsAny<AgentResponseRecord>(),
                It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task ResponseCallback_WhenStreamingDisabled_PersistsStandardResponse()
        {
            // Arrange
            var agentName = "TestAgent";
            var persistenceMock = new Mock<IAgentResponsePersistence>();

            // Create factory with StreamResponses = FALSE
            var factory = CreateFactory(persistenceMock.Object, streamResponses: false);

            factory.SetStepExecutionContext(new StepExecutionContext
            {
                StepExecutionId = Guid.NewGuid(),
                AgentConfigurationIds = new Dictionary<string, Guid> { { agentName, Guid.NewGuid() } }
            });

            var response = new ChatMessageContent(AuthorRole.Assistant, "Standard Content")
            {
                AuthorName = agentName,
                Metadata = new Dictionary<string, object> { { "OutputTokenCount", 42 } }
            };

            // Act
            await factory.InvokeResponseCallback(response);

            // Assert
            // Persistence SHOULD be called here
            persistenceMock.Verify(p => p.SaveAgentResponseAsync(
                It.Is<AgentResponseRecord>(r =>
                    r.AgentName == agentName &&
                    r.Content == "Standard Content" &&
                    r.OutputTokenCount == 42),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        private TestableOrchestrationFactory CreateFactory(IAgentResponsePersistence? persistence = null, bool streamResponses = true)
        {
            return new TestableOrchestrationFactory(
                new Mock<IAgentService>().Object,
                new Mock<IKernelBuilderService>().Object,
                Options.Create(new OrchestrationSettings { StreamResponses = streamResponses, WriteResponses = false }),
                new Mock<ILoggerFactory>().Object,
                persistence
            );
        }
    }
}
