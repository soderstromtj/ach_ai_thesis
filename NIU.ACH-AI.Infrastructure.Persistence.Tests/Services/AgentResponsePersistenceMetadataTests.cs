using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using NIU.ACH_AI.Application.DTOs;
using NIU.ACH_AI.Infrastructure.Persistence.Models;
using NIU.ACH_AI.Infrastructure.Persistence.Services;
using Xunit;

namespace NIU.ACH_AI.Infrastructure.Persistence.Tests.Services
{
    public class AgentResponsePersistenceMetadataTests
    {
        private readonly Mock<IServiceScopeFactory> _mockScopeFactory;
        private readonly Mock<IServiceScope> _mockScope;
        private readonly Mock<IServiceProvider> _mockServiceProvider;

        public AgentResponsePersistenceMetadataTests()
        {
            _mockScopeFactory = new Mock<IServiceScopeFactory>();
            _mockScope = new Mock<IServiceScope>();
            _mockServiceProvider = new Mock<IServiceProvider>();

            _mockScopeFactory.Setup(x => x.CreateScope()).Returns(_mockScope.Object);
            _mockScope.Setup(x => x.ServiceProvider).Returns(_mockServiceProvider.Object);
        }

        [Fact]
        public async Task SaveAgentResponseAsync_WithExtendedMetadata_PersistsAllFields()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<AchAIDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            // Setup DbContext factory
            _mockScopeFactory.Setup(x => x.CreateScope()).Returns(() => {
                var scope = new Mock<IServiceScope>();
                var serviceProvider = new Mock<IServiceProvider>();
                var context = new AchAIDbContext(options);
                serviceProvider.Setup(x => x.GetService(typeof(AchAIDbContext))).Returns(context);
                scope.Setup(x => x.ServiceProvider).Returns(serviceProvider.Object);
                return scope.Object;
            });

            var persistence = new AgentResponsePersistence(_mockScopeFactory.Object);

            var record = new AgentResponseRecord
            {
                StepExecutionId = Guid.NewGuid(),
                AgentConfigurationId = Guid.NewGuid(),
                AgentName = "TestAgent",
                Content = "Content",

                // Extended metadata
                CompletionId = "cmpl-test",
                ReasoningTokenCount = 123,
                OutputAudioTokenCount = 10,
                AcceptedPredictionTokenCount = 5,
                RejectedPredictionTokenCount = 2,
                InputAudioTokenCount = 20,
                CachedInputTokenCount = 300
            };

            // Act
            await persistence.SaveAgentResponseAsync(record);

            // Assert
            using (var context = new AchAIDbContext(options))
            {
                var saved = await context.AgentResponses.FirstOrDefaultAsync();
                Assert.NotNull(saved);
                Assert.Equal("cmpl-test", saved.CompletionId);
                Assert.Equal(123, saved.ReasoningTokenCount);
                Assert.Equal(10, saved.OutputAudioTokenCount);
                Assert.Equal(5, saved.AcceptedPredictionTokenCount);
                Assert.Equal(2, saved.RejectedPredictionTokenCount);
                Assert.Equal(20, saved.InputAudioTokenCount);
                Assert.Equal(300, saved.CachedInputTokenCount);
            }
        }
    }
}
