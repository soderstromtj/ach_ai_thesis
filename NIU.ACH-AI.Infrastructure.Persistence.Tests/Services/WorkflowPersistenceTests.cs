using Microsoft.EntityFrameworkCore;
using NIU.ACH_AI.Application.Configuration;
using NIU.ACH_AI.Infrastructure.Persistence.Models;
using NIU.ACH_AI.Infrastructure.Persistence.Services;

namespace NIU.ACH_AI.Infrastructure.Persistence.Tests.Services;

public class WorkflowPersistenceTests
{
    [Fact]
    public void Constructor_WithNullContext_ThrowsArgumentNullException()
    {
        var sagaContext = CreateSagaDbContext("Saga_Constructor_Test");
        var exception = Assert.Throws<ArgumentNullException>(() => new WorkflowPersistence(null!, sagaContext));
        Assert.Equal("context", exception.ParamName);
    }

    [Fact]
    public void Constructor_WithNullSagaContext_ThrowsArgumentNullException()
    {
        var context = CreateDbContext("Context_Constructor_Test");
        var exception = Assert.Throws<ArgumentNullException>(() => new WorkflowPersistence(context, null!));
        Assert.Equal("sagaContext", exception.ParamName);
    }

    [Fact]
    public async Task CreateScenarioAsync_WithValidContext_PersistsScenario()
    {
        using var context = CreateDbContext(nameof(CreateScenarioAsync_WithValidContext_PersistsScenario));
        using var sagaContext = CreateSagaDbContext("Saga_" + nameof(CreateScenarioAsync_WithValidContext_PersistsScenario));
        var service = new WorkflowPersistence(context, sagaContext);
        var scenarioContext = "test scenario";

        var scenarioId = await service.CreateScenarioAsync(scenarioContext);

        var stored = await context.Scenarios.FindAsync(scenarioId);
        Assert.NotNull(stored);
        Assert.Equal(scenarioContext, stored!.Context);
    }

    [Fact]
    public async Task CreateExperimentAsync_WithValidInput_PersistsExperiment()
    {
        using var context = CreateDbContext(nameof(CreateExperimentAsync_WithValidInput_PersistsExperiment));
        using var sagaContext = CreateSagaDbContext("Saga_" + nameof(CreateExperimentAsync_WithValidInput_PersistsExperiment));
        var service = new WorkflowPersistence(context, sagaContext);
        var config = CreateExperimentConfiguration();
        var scenarioId = Guid.NewGuid();

        var experimentId = await service.CreateExperimentAsync(config, scenarioId);

        var stored = await context.Experiments.FindAsync(experimentId);
        Assert.NotNull(stored);
        Assert.Equal(config.Name, stored!.ExperimentName);
    }

    [Fact]
    public async Task CreateStepExecutionAsync_WithValidInput_PersistsStepExecution()
    {
        using var context = CreateDbContext(nameof(CreateStepExecutionAsync_WithValidInput_PersistsStepExecution));
        using var sagaContext = CreateSagaDbContext("Saga_" + nameof(CreateStepExecutionAsync_WithValidInput_PersistsStepExecution));
        var service = new WorkflowPersistence(context, sagaContext);
        var stepConfig = CreateStepConfiguration();
        var experimentId = Guid.NewGuid();

        var result = await service.CreateStepExecutionAsync(experimentId, stepConfig);

        var stored = await context.StepExecutions.FindAsync(result.StepExecutionId);
        Assert.NotNull(stored);
        Assert.Equal(stepConfig.Name, stored!.AchStepName);
    }
    
    [Fact]
    public async Task GetSagaResultAsync_ReturnsNull_WhenSagaNotFound()
    {
        using var context = CreateDbContext(nameof(GetSagaResultAsync_ReturnsNull_WhenSagaNotFound));
        using var sagaContext = CreateSagaDbContext("Saga_" + nameof(GetSagaResultAsync_ReturnsNull_WhenSagaNotFound));
        var service = new WorkflowPersistence(context, sagaContext);
        
        var result = await service.GetSagaResultAsync(Guid.NewGuid());
        
        Assert.Null(result);
    }

    [Fact]
    public async Task GetSagaResultAsync_ReturnsResult_WhenSagaCompleted()
    {
        using var context = CreateDbContext(nameof(GetSagaResultAsync_ReturnsResult_WhenSagaCompleted));
        using var sagaContext = CreateSagaDbContext("Saga_" + nameof(GetSagaResultAsync_ReturnsResult_WhenSagaCompleted));
        var service = new WorkflowPersistence(context, sagaContext);
        
        var experimentId = Guid.NewGuid();
        var sagaState = new ExperimentState 
        { 
            CorrelationId = experimentId,
            CurrentState = "Completed",
            SerializedResult = "{\"Success\":true, \"ExperimentId\":\"" + experimentId + "\"}" 
        };
        
        sagaContext.Set<ExperimentState>().Add(sagaState);
        await sagaContext.SaveChangesAsync();

        var result = await service.GetSagaResultAsync(experimentId);
        
        Assert.NotNull(result);
        Assert.True(result!.Success);
        Assert.Equal(experimentId.ToString(), result.ExperimentId);
    }

    // Helpers

    private static AchAIDbContext CreateDbContext(string databaseName)
    {
        return new AchAIDbContext(new DbContextOptionsBuilder<AchAIDbContext>()
            .UseInMemoryDatabase(databaseName)
            .Options);
    }
    
    private static ACHSagaDbContext CreateSagaDbContext(string databaseName)
    {
        return new ACHSagaDbContext(new DbContextOptionsBuilder<ACHSagaDbContext>()
            .UseInMemoryDatabase(databaseName)
            .Options);
    }

    private static ExperimentConfiguration CreateExperimentConfiguration()
    {
        return new ExperimentConfiguration
        {
            Id = "exp-1",
            Name = "Test Experiment",
            ACHSteps = Array.Empty<ACHStepConfiguration>()
        };
    }

    private static ACHStepConfiguration CreateStepConfiguration()
    {
        return new ACHStepConfiguration
        {
            Id = 1,
            Name = "Hypothesis Brainstorming",
            TaskInstructions = "Task instructions"
        };
    }
}
