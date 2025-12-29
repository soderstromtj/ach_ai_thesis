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

            // Accessor for the metadata buffer for verification
            public ConcurrentDictionary<string, AgentResponseRecord> MetadataBuffers
            {
                get
                {
                    var field = typeof(BaseOrchestrationFactory<List<Evidence>, EvidenceResult>)
                        .GetField("_metadataBuffers", BindingFlags.NonPublic | BindingFlags.Instance);
                    return (ConcurrentDictionary<string, AgentResponseRecord>)field!.GetValue(this)!;
                }
            }

            // Helper to set StepExecutionContext via reflection since it is private
            public void SetStepExecutionContext(StepExecutionContext context)
            {
                var field = typeof(BaseOrchestrationFactory<List<Evidence>, EvidenceResult>)
                    .GetField("_stepExecutionContext", BindingFlags.NonPublic | BindingFlags.Instance);
                field!.SetValue(this, context);
            }

            // Abstract implementations (not used for these tests but required)
            protected override ILogger CreateLogger(ILoggerFactory loggerFactory) => new Mock<ILogger>().Object;
            protected override AgentOrchestration<string, EvidenceResult> CreateOrchestration(OrchestrationPromptInput input, List<string> agentNames, Kernel kernel, Agent[] agents, StructuredOutputTransform<EvidenceResult> outputTransform) => null!;
            protected override string GetResultTypeName() => "EvidenceResult";
            protected override List<Evidence> UnwrapResult(EvidenceResult wrapper) => new();
            protected override int GetItemCount(List<Evidence> result) => 0;
            protected override List<Evidence> CreateEmptyResult() => new();
            protected override List<Evidence> CreateErrorResult() => new();
            protected override string GetAgentSelectionReason(string? previousAgentName) => "Test";
        }

        [Fact]
        public async Task StreamingResponseCallback_WithFinalChunkAndMetadata_CachesMetadata()
        {
            // Arrange
            var agentName = "TestAgent";
            var completionId = "cmpl-123";
            var metadata = new Dictionary<string, object>
            {
                { "CompletionId", completionId },
                { "Usage", new Dictionary<string, object>
                    {
                        { "OutputTokenDetails", new Dictionary<string, object>
                            {
                                { "ReasoningTokenCount", 100 },
                                { "AudioTokenCount", 50 }
                            }
                        },
                        { "InputTokenDetails", new Dictionary<string, object>
                            {
                                { "AudioTokenCount", 25 },
                                { "CachedTokenCount", 200 }
                            }
                        }
                    }
                }
            };

            var response = new StreamingChatMessageContent(AuthorRole.Assistant, "Final chunk")
            {
                AuthorName = agentName,
                Metadata = metadata
            };

            var factory = CreateFactory();

            // Act
            await factory.InvokeStreamingResponseCallback(response, isFinal: true);

            // Assert
            Assert.True(factory.MetadataBuffers.ContainsKey(agentName));
            var cached = factory.MetadataBuffers[agentName];
            Assert.Equal(completionId, cached.CompletionId);
            Assert.Equal(100, cached.ReasoningTokenCount);
            Assert.Equal(50, cached.OutputAudioTokenCount);
            Assert.Equal(25, cached.InputAudioTokenCount);
            Assert.Equal(200, cached.CachedInputTokenCount);
        }

        [Fact]
        public async Task ResponseCallback_WithCachedMetadata_PassesMetadataToPersistence()
        {
            // Arrange
            var agentName = "TestAgent";
            var persistenceMock = new Mock<IAgentResponsePersistence>();
            var factory = CreateFactory(persistenceMock.Object);

            // Set up context
            var executionId = Guid.NewGuid();
            var configId = Guid.NewGuid();
            var context = new StepExecutionContext
            {
                StepExecutionId = executionId,
                AgentConfigurationIds = new Dictionary<string, Guid> { { agentName, configId } }
            };
            factory.SetStepExecutionContext(context);

            // 1. Simulate streaming completion to populate cache
            var metadata = new Dictionary<string, object>
            {
                { "CompletionId", "test-id" },
                { "Usage", new Dictionary<string, object>
                    {
                        { "OutputTokenDetails", new Dictionary<string, object> { { "ReasoningTokenCount", 99 } } }
                    }
                }
            };
            var streamingResponse = new StreamingChatMessageContent(AuthorRole.Assistant, "")
            {
                AuthorName = agentName,
                Metadata = metadata
            };
            await factory.InvokeStreamingResponseCallback(streamingResponse, isFinal: true);

            // 2. Simulate final response callback
            var finalResponse = new ChatMessageContent(AuthorRole.Assistant, "Content")
            {
                AuthorName = agentName
            };

            // Act
            await factory.InvokeResponseCallback(finalResponse);

            // Assert
            persistenceMock.Verify(p => p.SaveAgentResponseAsync(
                It.Is<AgentResponseRecord>(r =>
                    r.AgentName == agentName &&
                    r.CompletionId == "test-id" &&
                    r.ReasoningTokenCount == 99),
                It.IsAny<CancellationToken>()), Times.Once);

            // Verify cache is cleared
            Assert.False(factory.MetadataBuffers.ContainsKey(agentName));
        }

        private TestableOrchestrationFactory CreateFactory(IAgentResponsePersistence? persistence = null)
        {
            return new TestableOrchestrationFactory(
                new Mock<IAgentService>().Object,
                new Mock<IKernelBuilderService>().Object,
                Options.Create(new OrchestrationSettings { StreamResponses = true, WriteResponses = false }),
                new Mock<ILoggerFactory>().Object,
                persistence
            );
        }
    }
}
#pragma warning restore SKEXP0110
