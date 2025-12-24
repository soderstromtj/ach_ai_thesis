# Repository Usage Guide

This guide shows how to use the repository layer to persist AI-generated domain entities to the database.

## Step 1: Register Repositories in DI Container

In `Program.cs`, add this method:

```csharp
private static void RegisterDBServices(IServiceCollection services, IConfiguration configuration)
{
    // Register DbContext
    services.AddDbContext<AchAIDbContext>(options => {
        var connectionString = configuration.GetConnectionString("AchAiDBConnection");
        options.UseSqlServer(connectionString);
    });

    // Register repositories
    services.AddScoped<IHypothesisRepository, HypothesisRepository>();
    services.AddScoped<IEvidenceRepository, EvidenceRepository>();
    services.AddScoped<IEvidenceHypothesisEvaluationRepository, EvidenceHypothesisEvaluationRepository>();
}
```

Then call it in `CreateHostBuilder`:

```csharp
.ConfigureServices((context, services) =>
{
    RegisterExperimentConfigurations(services, context.Configuration);
    RegisterKernelServices(services);
    RegisterLogging(services, context.Configuration);
    RegisterDBServices(services, context.Configuration); // Add this line
});
```

## Step 2: Use Repositories in Your Code

### Example: Saving Hypotheses

```csharp
// After AI generates hypotheses
var hypotheses = await ExecuteHypothesisBrainstormingAsync(host, achStepConfig, input);

// Save to database
var hypothesisRepo = host.Services.GetRequiredService<IHypothesisRepository>();
await hypothesisRepo.SaveBatchAsync(hypotheses, stepExecutionId);

// Later, retrieve them
var savedHypotheses = await hypothesisRepo.GetByStepExecutionIdAsync(stepExecutionId);
```

### Example: Saving Evidence

```csharp
// After AI extracts evidence
var evidenceList = await ExecuteEvidenceExtractionAsync(host, achStepConfig, input);

// Save to database
var evidenceRepo = host.Services.GetRequiredService<IEvidenceRepository>();
await evidenceRepo.SaveBatchAsync(evidenceList, stepExecutionId);

// Later, retrieve them
var savedEvidence = await evidenceRepo.GetByStepExecutionIdAsync(stepExecutionId);
```

### Example: Saving Evaluations (More Complex)

```csharp
// After AI generates evaluations
var evaluations = await ExecuteEvidenceHypothesisEvaluationAsync(host, achStepConfig, input);

// First, save hypotheses and evidence, then build ID maps
var hypothesisRepo = host.Services.GetRequiredService<IHypothesisRepository>();
var evidenceRepo = host.Services.GetRequiredService<IEvidenceRepository>();

await hypothesisRepo.SaveBatchAsync(refinedHypotheses, stepExecutionId);
await evidenceRepo.SaveBatchAsync(evidenceList, stepExecutionId);

// Build ID maps (map business key -> database ID)
var hypothesisIdMap = new Dictionary<string, Guid>();
var savedHypotheses = await hypothesisRepo.GetByStepExecutionIdAsync(stepExecutionId);
foreach (var h in savedHypotheses)
{
    // Use ShortTitle as the business key
    hypothesisIdMap[h.ShortTitle] = h.HypothesisId;
}

var evidenceIdMap = new Dictionary<string, Guid>();
var savedEvidence = await evidenceRepo.GetByStepExecutionIdAsync(stepExecutionId);
foreach (var e in savedEvidence)
{
    // Use Claim as the business key
    evidenceIdMap[e.Claim] = e.EvidenceId;
}

// Now save the evaluations
var evaluationRepo = host.Services.GetRequiredService<IEvidenceHypothesisEvaluationRepository>();
await evaluationRepo.SaveBatchAsync(
    evaluations,
    stepExecutionId,
    hypothesisIdMap,
    evidenceIdMap);
```

## Step 3: Complete Workflow Example

Here's a complete example in `RunOrchestrationAsync`:

```csharp
private static async Task RunOrchestrationAsync(IHost host, ExperimentConfiguration experimentConfig)
{
    var loggerFactory = host.Services.GetRequiredService<ILoggerFactory>();
    var stepExecutionId = Guid.NewGuid(); // Generate step execution ID

    // Build input
    var input = new OrchestrationPromptInput
    {
        KeyQuestion = experimentConfig.KeyQuestion,
        Context = experimentConfig.Context,
        TaskInstructions = experimentConfig.ACHSteps[0].TaskInstructions,
    };

    // 1. Generate and save hypotheses
    var hypotheses = await ExecuteHypothesisBrainstormingAsync(host, experimentConfig.ACHSteps[0], input);
    var hypothesisRepo = host.Services.GetRequiredService<IHypothesisRepository>();
    await hypothesisRepo.SaveBatchAsync(hypotheses, stepExecutionId);
    Console.WriteLine($"Saved {hypotheses.Count} hypotheses to database");

    // 2. Refine and update hypotheses
    input.HypothesisResult = new HypothesisResult { Hypotheses = hypotheses };
    var refinedHypotheses = await ExecuteHypothesisEvaluationAsync(host, experimentConfig.ACHSteps[1], input);

    // Mark refined hypotheses in database
    var savedHypotheses = await hypothesisRepo.GetByStepExecutionIdAsync(stepExecutionId);
    foreach (var refined in refinedHypotheses)
    {
        var match = savedHypotheses.FirstOrDefault(h => h.ShortTitle == refined.ShortTitle);
        if (match != null)
        {
            await hypothesisRepo.MarkAsRefinedAsync(match.HypothesisId);
        }
    }

    // 3. Extract and save evidence
    var evidenceList = await ExecuteEvidenceExtractionAsync(host, experimentConfig.ACHSteps[2], input);
    var evidenceRepo = host.Services.GetRequiredService<IEvidenceRepository>();
    await evidenceRepo.SaveBatchAsync(evidenceList, stepExecutionId);
    Console.WriteLine($"Saved {evidenceList.Count} evidence items to database");

    // 4. Evaluate and save evaluations
    var evaluations = new List<EvidenceHypothesisEvaluation>();
    foreach (var evidence in evidenceList)
    {
        foreach (var hypothesis in refinedHypotheses)
        {
            input.EvidenceResult = new EvidenceResult { Evidence = new List<Evidence> { evidence } };
            input.HypothesisResult = new HypothesisResult { Hypotheses = new List<Hypothesis> { hypothesis } };

            var evalResults = await ExecuteEvidenceHypothesisEvaluationAsync(host, experimentConfig.ACHSteps[3], input);
            evaluations.AddRange(evalResults);
        }
    }

    // Build ID maps and save evaluations
    var hypothesisIdMap = savedHypotheses.ToDictionary(h => h.ShortTitle, h => h.HypothesisId);
    var evidenceIdMap = (await evidenceRepo.GetByStepExecutionIdAsync(stepExecutionId))
        .ToDictionary(e => e.Claim, e => e.EvidenceId);

    var evaluationRepo = host.Services.GetRequiredService<IEvidenceHypothesisEvaluationRepository>();
    await evaluationRepo.SaveBatchAsync(evaluations, stepExecutionId, hypothesisIdMap, evidenceIdMap);
    Console.WriteLine($"Saved {evaluations.Count} evaluations to database");
}
```

## Key Benefits

1. **Clean Separation**: FrontendConsole only works with domain entities
2. **No Ambiguous References**: Never imports `Infrastructure.Persistence.Models`
3. **Simple Mapping**: Mappers handle conversion in one place
4. **Testable**: Can mock repositories for unit testing
5. **Maintainable**: Changes to DB schema only affect mappers and repositories

## Important Notes

- Always save hypotheses and evidence BEFORE saving evaluations
- Use business keys (ShortTitle, Claim) to build ID maps for evaluations
- StepExecutionId tracks which AI run produced which results
- All timestamps (CreatedAt) are set automatically by mappers
