using FluentAssertions;
using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using NIU.ACH_AI.Infrastructure.Extensions;
using Xunit;

namespace NIU.ACH_AI.Infrastructure.Tests.Extensions
{
    /// <summary>
    /// Tests for the MassTransit configuration extension.
    /// </summary>
    public class MessageBrokerExtensionsTests
    {
        /// <summary>
        /// Verifies that AddMessageBroker correctly registers the necessary MassTransit services.
        /// </summary>
        [Fact]
        public void AddMessageBroker_RegistersMassTransitBus()
        {
            // Arrange
            var services = new ServiceCollection();
            var configurationMock = new Mock<IConfiguration>();
            
            // Setup configuration to return nulls (defaults) or specific values
            configurationMock.Setup(c => c["RabbitMQ:Host"]).Returns("localhost");
            
            // Act
            services.AddMessageBroker(configurationMock.Object);
            var provider = services.BuildServiceProvider();

            // Assert
            // IBus is the primary interface used by publishers
            var bus = provider.GetService<IBus>();
            bus.Should().NotBeNull("IBus should be registered in the service collection");

            // IBusControl is used to start/stop the bus
            var busControl = provider.GetService<IBusControl>();
            busControl.Should().NotBeNull("IBusControl should be registered for lifecycle management");
        }
    }
}
