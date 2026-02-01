using FluentAssertions;
using MassTransit;
using Microsoft.Extensions.Logging;
using Moq;
using NIU.ACH_AI.Application.DTOs;
using NIU.ACH_AI.Application.Messaging.Events;
using NIU.ACH_AI.Infrastructure.Messaging.Adapters;

namespace NIU.ACH_AI.Infrastructure.Tests.Messaging;

public class MessagingTests
{
    private readonly Mock<IPublishEndpoint> _mockPublishEndpoint;
    private readonly Mock<ILogger<MessagingAgentResponsePersistence>> _mockAdapterLogger;

    public MessagingTests()
    {
        _mockPublishEndpoint = new Mock<IPublishEndpoint>();
        _mockAdapterLogger = new Mock<ILogger<MessagingAgentResponsePersistence>>();
    }

/*
    [Fact]
    public async Task MessagingAgentResponsePersistence_SaveRecord_PublishesEvent()
    {
        // Arrange
        var adapter = new MessagingAgentResponsePersistence(_mockPublishEndpoint.Object, _mockAdapterLogger.Object);
        var record = new AgentResponseRecord
        {
            StepExecutionId = Guid.NewGuid(),
            AgentConfigurationId = Guid.NewGuid(),
            AgentName = "TestAgent",
            Content = "Test Content",
            CreatedAt = DateTime.UtcNow
        };

        // Act
        await adapter.SaveAgentResponseAsync(record);

        // Assert
        _mockPublishEndpoint.Verify(x => x.Publish<IAgentResponseReceived>(
            It.Is<object>(obj => 
                ((dynamic)obj).AgentName == "TestAgent" && 
                ((dynamic)obj).Content == "Test Content"
            ), 
            It.IsAny<CancellationToken>()), Times.Once);
    }
*/


}
