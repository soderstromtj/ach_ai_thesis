using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using NIU.ACH_AI.Application.DTOs;
using NIU.ACH_AI.Infrastructure.Persistence.Models;
using NIU.ACH_AI.Infrastructure.Persistence.Services;
using Xunit;

namespace NIU.ACH_AI.Infrastructure.Persistence.Tests.Services
{
    public class AgentResponsePersistenceTests
    {
        private readonly Mock<IServiceScopeFactory> _mockScopeFactory;
        private readonly Mock<IServiceScope> _mockScope;
        private readonly Mock<IServiceProvider> _mockServiceProvider;

        public AgentResponsePersistenceTests()
        {
            _mockScopeFactory = new Mock<IServiceScopeFactory>();
            _mockScope = new Mock<IServiceScope>();
            _mockServiceProvider = new Mock<IServiceProvider>();

            _mockScopeFactory.Setup(x => x.CreateScope()).Returns(_mockScope.Object);
            _mockScope.Setup(x => x.ServiceProvider).Returns(_mockServiceProvider.Object);
        }

        [Fact]
        public async Task SaveAgentResponseAsync_ShouldBeThreadSafe()
        {
            // Arrange
            // Create a real DbContext with InMemory database for each scope
            var options = new DbContextOptionsBuilder<AchAIDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()) // Unique DB per test
                .Options;

            // We need a way to return a NEW context for each scope, but sharing the same underlying InMemory DB
            // To simulate the real world where multiple contexts connect to the same SQL DB.
            // In InMemory, the database is identified by the name.

            // Setup the mock factory to return a new context instance pointing to the SAME in-memory DB
            // every time CreateScope -> GetService is called.

            _mockScopeFactory.Setup(x => x.CreateScope()).Returns(() => {
                var scope = new Mock<IServiceScope>();
                var serviceProvider = new Mock<IServiceProvider>();

                var context = new AchAIDbContext(options);
                serviceProvider.Setup(x => x.GetService(typeof(AchAIDbContext))).Returns(context);

                scope.Setup(x => x.ServiceProvider).Returns(serviceProvider.Object);
                return scope.Object;
            });

            var persistence = new AgentResponsePersistence(_mockScopeFactory.Object);

            var tasks = new List<Task>();
            int numberOfThreads = 10;
            var barrier = new Barrier(numberOfThreads);

            for (int i = 0; i < numberOfThreads; i++)
            {
                tasks.Add(Task.Run(async () =>
                {
                    var record = new AgentResponseRecord
                    {
                        StepExecutionId = Guid.NewGuid(),
                        AgentConfigurationId = Guid.NewGuid(),
                        AgentName = $"Agent-{Guid.NewGuid()}",
                        Content = "Test Content",
                        CreatedAt = DateTime.UtcNow
                    };

                    // Synchronize threads to start roughly at the same time
                    barrier.SignalAndWait();

                    await persistence.SaveAgentResponseAsync(record);
                }));
            }

            // Act
            await Task.WhenAll(tasks);

            // Assert
            // Verify that all records were saved
            using (var context = new AchAIDbContext(options))
            {
                Assert.Equal(numberOfThreads, await context.AgentResponses.CountAsync());
            }
        }
    }
}
