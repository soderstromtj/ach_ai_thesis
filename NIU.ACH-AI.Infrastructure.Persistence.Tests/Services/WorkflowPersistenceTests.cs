using Microsoft.EntityFrameworkCore;
using NIU.ACH_AI.Application.Configuration;
using NIU.ACH_AI.Infrastructure.Persistence.Models;
using NIU.ACH_AI.Infrastructure.Persistence.Services;

namespace NIU.ACH_AI.Infrastructure.Persistence.Tests.Services;

/// <summary>
/// Comprehensive unit tests for WorkflowPersistence.
///
/// Testing Strategy:
/// -----------------
/// WorkflowPersistence is an infrastructure service that writes workflow metadata
/// using EF Core and returns identifiers to the application layer.
/// Tests use the EF Core InMemory provider to keep execution fast and isolated.
///
/// What We Can Test:
/// 1. Constructor - Validates dependency injection works correctly
/// 2. CreateScenarioAsync - Validates input, persistence, and error handling
/// 3. CreateExperimentAsync - Validates input, persistence, and error handling
/// 4. CreateStepExecutionAsync - Validates input, persistence, and error handling
/// 5. UpdateStepExecutionStatusAsync - Validates input, updates, and error handling
///
/// Testing Challenges:
/// We avoid a real database and simulate persistence failures by overriding
/// SaveChangesAsync in a test context, keeping behavior deterministic and isolated.
/// </summary>
public class WorkflowPersistenceTests
{
    /// <summary>
    /// This test verifies the constructor throws when the DbContext is null.
    /// </summary>
    [Fact]
    public void Constructor_WithNullContext_ThrowsArgumentNullException()
    {
        // Arrange, Act
        // Assert
        var exception = Assert.Throws<ArgumentNullException>(() => new WorkflowPersistence(null!));
        Assert.Equal("context", exception.ParamName);
    }

    /// <summary>
    /// This test verifies CreateScenarioAsync rejects empty context input.
    /// </summary>
    [Fact]
    public async Task CreateScenarioAsync_WithEmptyContext_ThrowsArgumentException()
    {
        // Arrange
        using var context = CreateDbContext(nameof(CreateScenarioAsync_WithEmptyContext_ThrowsArgumentException));
        var service = new WorkflowPersistence(context);

        // Act
        // Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(() => service.CreateScenarioAsync("  "));
        Assert.Equal("context", exception.ParamName);
    }

    /// <summary>
    /// This test verifies CreateScenarioAsync persists a scenario and returns its ID.
    /// </summary>
    [Fact]
    public async Task CreateScenarioAsync_WithValidContext_PersistsScenario()
    {
        // Arrange
        using var context = CreateDbContext(nameof(CreateScenarioAsync_WithValidContext_PersistsScenario));
        var service = new WorkflowPersistence(context);
        var scenarioContext = "test scenario";

        // Act
        var scenarioId = await service.CreateScenarioAsync(scenarioContext);

        // Assert
        var stored = await context.Scenarios.FindAsync(scenarioId);
        Assert.NotNull(stored);
        Assert.Equal(scenarioContext, stored!.Context);
    }

    /// <summary>
    /// This test verifies CreateScenarioAsync wraps persistence failures with a clear exception.
    /// </summary>
    [Fact]
    public async Task CreateScenarioAsync_WhenSaveFails_ThrowsInvalidOperationException()
    {
        // Arrange
        using var context = new FailingAchAIDbContext(CreateOptions(nameof(CreateScenarioAsync_WhenSaveFails_ThrowsInvalidOperationException)));
        var service = new WorkflowPersistence(context);

        // Act
        // Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateScenarioAsync("context"));
        Assert.Equal("Failed to persist scenario.", exception.Message);
    }

    /// <summary>
    /// This test verifies CreateExperimentAsync rejects null configuration input.
    /// </summary>
    [Fact]
    public async Task CreateExperimentAsync_WithNullConfiguration_ThrowsArgumentNullException()
    {
        // Arrange
        using var context = CreateDbContext(nameof(CreateExperimentAsync_WithNullConfiguration_ThrowsArgumentNullException));
        var service = new WorkflowPersistence(context);

        // Act
        // Assert
        var exception = await Assert.ThrowsAsync<ArgumentNullException>(() => service.CreateExperimentAsync(null!, Guid.NewGuid()));
        Assert.Equal("configuration", exception.ParamName);
    }

    /// <summary>
    /// This test verifies CreateExperimentAsync rejects an empty scenario ID.
    /// </summary>
    [Fact]
    public async Task CreateExperimentAsync_WithEmptyScenarioId_ThrowsArgumentException()
    {
        // Arrange
        using var context = CreateDbContext(nameof(CreateExperimentAsync_WithEmptyScenarioId_ThrowsArgumentException));
        var service = new WorkflowPersistence(context);
        var config = CreateExperimentConfiguration();

        // Act
        // Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(() => service.CreateExperimentAsync(config, Guid.Empty));
        Assert.Equal("scenarioId", exception.ParamName);
    }

    /// <summary>
    /// This test verifies CreateExperimentAsync persists an experiment and returns its ID.
    /// </summary>
    [Fact]
    public async Task CreateExperimentAsync_WithValidInput_PersistsExperiment()
    {
        // Arrange
        using var context = CreateDbContext(nameof(CreateExperimentAsync_WithValidInput_PersistsExperiment));
        var service = new WorkflowPersistence(context);
        var config = CreateExperimentConfiguration();
        var scenarioId = Guid.NewGuid();

        // Act
        var experimentId = await service.CreateExperimentAsync(config, scenarioId);

        // Assert
        var stored = await context.Experiments.FindAsync(experimentId);
        Assert.NotNull(stored);
        Assert.Equal(config.Name, stored!.ExperimentName);
        Assert.Equal(config.Description, stored.Description);
        Assert.Equal(config.KeyQuestion, stored.Kiq);
        Assert.Equal(scenarioId, stored.ScenarioId);
    }

    /// <summary>
    /// This test verifies CreateExperimentAsync wraps persistence failures with a clear exception.
    /// </summary>
    [Fact]
    public async Task CreateExperimentAsync_WhenSaveFails_ThrowsInvalidOperationException()
    {
        // Arrange
        using var context = new FailingAchAIDbContext(CreateOptions(nameof(CreateExperimentAsync_WhenSaveFails_ThrowsInvalidOperationException)));
        var service = new WorkflowPersistence(context);
        var config = CreateExperimentConfiguration();

        // Act
        // Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateExperimentAsync(config, Guid.NewGuid()));
        Assert.Equal("Failed to persist experiment.", exception.Message);
    }

    /// <summary>
    /// This test verifies CreateStepExecutionAsync rejects null step configuration input.
    /// </summary>
    [Fact]
    public async Task CreateStepExecutionAsync_WithNullStepConfiguration_ThrowsArgumentNullException()
    {
        // Arrange
        using var context = CreateDbContext(nameof(CreateStepExecutionAsync_WithNullStepConfiguration_ThrowsArgumentNullException));
        var service = new WorkflowPersistence(context);

        // Act
        // Assert
        var exception = await Assert.ThrowsAsync<ArgumentNullException>(() => service.CreateStepExecutionAsync(Guid.NewGuid(), null!));
        Assert.Equal("stepConfiguration", exception.ParamName);
    }

    /// <summary>
    /// This test verifies CreateStepExecutionAsync rejects an empty experiment ID.
    /// </summary>
    [Fact]
    public async Task CreateStepExecutionAsync_WithEmptyExperimentId_ThrowsArgumentException()
    {
        // Arrange
        using var context = CreateDbContext(nameof(CreateStepExecutionAsync_WithEmptyExperimentId_ThrowsArgumentException));
        var service = new WorkflowPersistence(context);
        var stepConfig = CreateStepConfiguration();

        // Act
        // Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(() => service.CreateStepExecutionAsync(Guid.Empty, stepConfig));
        Assert.Equal("experimentId", exception.ParamName);
    }

    /// <summary>
    /// This test verifies CreateStepExecutionAsync persists a step execution and returns context.
    /// </summary>
    [Fact]
    public async Task CreateStepExecutionAsync_WithValidInput_PersistsStepExecution()
    {
        // Arrange
        using var context = CreateDbContext(nameof(CreateStepExecutionAsync_WithValidInput_PersistsStepExecution));
        var service = new WorkflowPersistence(context);
        var stepConfig = CreateStepConfiguration();
        var experimentId = Guid.NewGuid();

        // Act
        var result = await service.CreateStepExecutionAsync(experimentId, stepConfig);

        // Assert
        var stored = await context.StepExecutions.FindAsync(result.StepExecutionId);
        Assert.NotNull(stored);
        Assert.Equal(experimentId, stored!.ExperimentId);
        Assert.Equal(stepConfig.Id, stored.AchStepId);
        Assert.Equal(stepConfig.Name, stored.AchStepName);
        Assert.Equal(stepConfig.Description, stored.Description);
        Assert.Equal(stepConfig.TaskInstructions, stored.TaskInstructions);
        Assert.Equal(stepConfig.Id, result.AchStepId);
        Assert.Equal(stepConfig.Name, result.AchStepName);
        Assert.Equal(experimentId, result.ExperimentId);
    }

    /// <summary>
    /// This test verifies CreateStepExecutionAsync wraps persistence failures with a clear exception.
    /// </summary>
    [Fact]
    public async Task CreateStepExecutionAsync_WhenSaveFails_ThrowsInvalidOperationException()
    {
        // Arrange
        using var context = new FailingAchAIDbContext(CreateOptions(nameof(CreateStepExecutionAsync_WhenSaveFails_ThrowsInvalidOperationException)));
        var service = new WorkflowPersistence(context);

        // Act
        // Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateStepExecutionAsync(Guid.NewGuid(), CreateStepConfiguration()));
        Assert.Equal("Failed to persist step execution.", exception.Message);
    }

    /// <summary>
    /// This test verifies UpdateStepExecutionStatusAsync rejects an empty step execution ID.
    /// </summary>
    [Fact]
    public async Task UpdateStepExecutionStatusAsync_WithEmptyStepExecutionId_ThrowsArgumentException()
    {
        // Arrange
        using var context = CreateDbContext(nameof(UpdateStepExecutionStatusAsync_WithEmptyStepExecutionId_ThrowsArgumentException));
        var service = new WorkflowPersistence(context);

        // Act
        // Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(() => service.UpdateStepExecutionStatusAsync(Guid.Empty, "Running"));
        Assert.Equal("stepExecutionId", exception.ParamName);
    }

    /// <summary>
    /// This test verifies UpdateStepExecutionStatusAsync rejects an empty status value.
    /// </summary>
    [Fact]
    public async Task UpdateStepExecutionStatusAsync_WithEmptyStatus_ThrowsArgumentException()
    {
        // Arrange
        using var context = CreateDbContext(nameof(UpdateStepExecutionStatusAsync_WithEmptyStatus_ThrowsArgumentException));
        var service = new WorkflowPersistence(context);

        // Act
        // Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(() => service.UpdateStepExecutionStatusAsync(Guid.NewGuid(), " "));
        Assert.Equal("status", exception.ParamName);
    }

    /// <summary>
    /// This test verifies UpdateStepExecutionStatusAsync throws when the step execution is missing.
    /// </summary>
    [Fact]
    public async Task UpdateStepExecutionStatusAsync_WithMissingStepExecution_ThrowsInvalidOperationException()
    {
        // Arrange
        using var context = CreateDbContext(nameof(UpdateStepExecutionStatusAsync_WithMissingStepExecution_ThrowsInvalidOperationException));
        var service = new WorkflowPersistence(context);

        // Act
        // Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => service.UpdateStepExecutionStatusAsync(Guid.NewGuid(), "Running"));
        Assert.Contains("StepExecution", exception.Message);
        Assert.Contains("not found", exception.Message);
    }

    /// <summary>
    /// This test verifies UpdateStepExecutionStatusAsync updates status and metadata fields.
    /// </summary>
    [Fact]
    public async Task UpdateStepExecutionStatusAsync_WithValidInput_UpdatesFields()
    {
        // Arrange
        using var context = CreateDbContext(nameof(UpdateStepExecutionStatusAsync_WithValidInput_UpdatesFields));
        var service = new WorkflowPersistence(context);
        var stepExecution = await SeedStepExecutionAsync(context);
        var start = DateTime.UtcNow.AddMinutes(-1);
        var end = DateTime.UtcNow;

        // Act
        await service.UpdateStepExecutionStatusAsync(
            stepExecution.StepExecutionId,
            "Failed",
            start,
            end,
            "TestError",
            "Test message",
            2);

        // Assert
        var stored = await context.StepExecutions.FindAsync(stepExecution.StepExecutionId);
        Assert.NotNull(stored);
        Assert.Equal("Failed", stored!.ExecutionStatus);
        Assert.Equal(start, stored.DatetimeStart);
        Assert.Equal(end, stored.DatetimeEnd);
        Assert.Equal("TestError", stored.ErrorType);
        Assert.Equal("Test message", stored.ErrorMessage);
        Assert.Equal(2, stored.RetryCount);
    }

    /// <summary>
    /// This test verifies UpdateStepExecutionStatusAsync wraps persistence failures with a clear exception.
    /// </summary>
    [Fact]
    public async Task UpdateStepExecutionStatusAsync_WhenSaveFails_ThrowsInvalidOperationException()
    {
        // Arrange
        var options = CreateOptions(nameof(UpdateStepExecutionStatusAsync_WhenSaveFails_ThrowsInvalidOperationException));
        using var seedContext = new AchAIDbContext(options);
        var seeded = await SeedStepExecutionAsync(seedContext);
        using var failingContext = new FailingAchAIDbContext(options);
        var service = new WorkflowPersistence(failingContext);

        // Act
        // Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => service.UpdateStepExecutionStatusAsync(seeded.StepExecutionId, "Completed"));
        Assert.Equal("Failed to update step execution status.", exception.Message);
    }

    private static AchAIDbContext CreateDbContext(string databaseName)
    {
        return new AchAIDbContext(CreateOptions(databaseName));
    }

    private static DbContextOptions<AchAIDbContext> CreateOptions(string databaseName)
    {
        return new DbContextOptionsBuilder<AchAIDbContext>()
            .UseInMemoryDatabase(databaseName)
            .Options;
    }

    private static ExperimentConfiguration CreateExperimentConfiguration()
    {
        return new ExperimentConfiguration
        {
            Id = "exp-1",
            Name = "Test Experiment",
            Description = "Test experiment description",
            KeyQuestion = "Test key question",
            Context = "Test context",
            ACHSteps = Array.Empty<ACHStepConfiguration>()
        };
    }

    private static ACHStepConfiguration CreateStepConfiguration()
    {
        return new ACHStepConfiguration
        {
            Id = 1,
            Name = "Hypothesis Brainstorming",
            Description = "Step description",
            TaskInstructions = "Task instructions"
        };
    }

    private static async Task<StepExecution> SeedStepExecutionAsync(AchAIDbContext context)
    {
        var stepExecution = new StepExecution
        {
            StepExecutionId = Guid.NewGuid(),
            ExperimentId = Guid.NewGuid(),
            AchStepId = 1,
            AchStepName = "Hypothesis Brainstorming",
            ExecutionStatus = "NotStarted",
            RetryCount = 0
        };

        context.StepExecutions.Add(stepExecution);
        await context.SaveChangesAsync();
        return stepExecution;
    }

    private sealed class FailingAchAIDbContext : AchAIDbContext
    {
        public FailingAchAIDbContext(DbContextOptions<AchAIDbContext> options)
            : base(options)
        {
        }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            throw new DbUpdateException("Simulated failure.");
        }
    }
}
