using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NIU.ACH_AI.Application.Interfaces;
using Xunit;

namespace NIU.ACH_AI.FrontendConsole.Tests.Configuration
{
    public class DependencyInjectionTests
    {
        [Fact]
        public void AddFrontendServices_ShouldRegisterServicesCorrectly()
        {
            // Arrange
            var hostBuilder = Host.CreateDefaultBuilder()
                .ConfigureServices((context, services) =>
                {
                    // We need a dummy configuration
                    // But AddFrontendServices expects specific sections "AIServiceSettings" etc.
                    // Let's rely on default configuration or mock it? 
                    // Extension method `AddFrontendServices` uses `configuration.GetSection(...)`
                    
                    // We can use the ConfigurationBuilder to provide in-memory collection
                    var config = new ConfigurationBuilder()
                        .AddInMemoryCollection(new Dictionary<string, string>
                        {
                            {"AIServiceSettings:ApiKey", "test-key"},
                            {"RabbitMQ:Host", "localhost"}
                        })
                        .Build();
                    
                    // Replace the context.Configuration with our own if possible, 
                    // or just pass it to the method.
                    // Actually AddFrontendServices(this IServiceCollection services, IConfiguration configuration)
                    
                    // Register required persistence mocks
                    services.AddSingleton(new Moq.Mock<IWorkflowPersistence>().Object);
                    services.AddSingleton(new Moq.Mock<IAgentConfigurationPersistence>().Object);
                    services.AddSingleton(new Moq.Mock<IWorkflowResultPersistence>().Object);
                    services.AddSingleton(new Moq.Mock<IAgentResponsePersistence>().Object);

                   NIU.ACH_AI.FrontendConsole.Extensions.DependencyInjection.AddFrontendServices(services, config);
                });

            var host = hostBuilder.Build();

            // Assert
            // 1. Check ACHWorkflowCoordinator
            var coordinator = host.Services.GetService<IACHWorkflowCoordinator>();
            coordinator.Should().NotBeNull("IACHWorkflowCoordinator should be resolvable via AddFrontendServices");

            // 2. Check MassTransit
            var bus = host.Services.GetService<MassTransit.IBus>();
            bus.Should().NotBeNull("MassTransit IBus should be resolvable");
        }
    }
}
