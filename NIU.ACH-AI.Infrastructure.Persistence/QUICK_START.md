# Quick Start Guide

## Files Created

```
NIU.ACH-AI.Infrastructure.Persistence/
├── Mappers/
│   ├── HypothesisMapper.cs                          ← Domain ↔ DB conversion
│   ├── EvidenceMapper.cs                            ← Domain ↔ DB conversion
│   └── EvidenceHypothesisEvaluationMapper.cs        ← Domain ↔ DB conversion
├── Repositories/
│   ├── IHypothesisRepository.cs                     ← Public interface
│   ├── HypothesisRepository.cs                      ← Implementation
│   ├── IEvidenceRepository.cs                       ← Public interface
│   ├── EvidenceRepository.cs                        ← Implementation
│   ├── IEvidenceHypothesisEvaluationRepository.cs  ← Public interface
│   └── EvidenceHypothesisEvaluationRepository.cs   ← Implementation
├── Models/                                          ← EF scaffolded (existing)
│   ├── Hypothesis.cs
│   ├── Evidence.cs
│   ├── EvidenceHypothesisEvaluation.cs
│   └── AchAIDbContext.cs
├── ARCHITECTURE.md                                  ← Design documentation
├── USAGE_EXAMPLE.md                                 ← Integration examples
└── QUICK_START.md                                   ← This file
```

## How It Works (5-Minute Overview)

### 1. **The Problem**
```csharp
// ❌ BEFORE: Ambiguous references
using NIU.ACH_AI.Domain.Entities;
using NIU.ACH_AI.Infrastructure.Persistence.Models;

List<Hypothesis> hypotheses = ...;  // ERROR: Which Hypothesis?
```

### 2. **The Solution**
```csharp
// ✅ AFTER: Clean separation via repositories
using NIU.ACH_AI.Domain.Entities;
// No Persistence.Models import needed!

// AI generates domain entities
var hypotheses = await factory.ExecuteCoreAsync(input);

// Repository saves them (handles mapping internally)
var repo = host.Services.GetRequiredService<IHypothesisRepository>();
await repo.SaveBatchAsync(hypotheses, stepExecutionId);
```

### 3. **Under the Hood**

```
Your Code (FrontendConsole)
    ↓
    Uses Domain.Entities.Hypothesis
    ↓
IHypothesisRepository.SaveBatchAsync(domain entities)
    ↓
HypothesisRepository (implementation)
    ↓
HypothesisMapper.ToDatabase(domain → db)
    ↓
Persistence.Models.Hypothesis (EF entity)
    ↓
DbContext.SaveChangesAsync()
    ↓
SQL Server
```

## 3-Step Integration

### Step 1: Register Services

Add to `Program.cs`:

```csharp
using NIU.ACH_AI.Infrastructure.Persistence.Repositories;
using NIU.ACH_AI.Infrastructure.Persistence.Models;
using Microsoft.EntityFrameworkCore;

private static void RegisterDBServices(IServiceCollection services, IConfiguration configuration)
{
    services.AddDbContext<AchAIDbContext>(options =>
        options.UseSqlServer(configuration.GetConnectionString("AchAiDBConnection")));

    services.AddScoped<IHypothesisRepository, HypothesisRepository>();
    services.AddScoped<IEvidenceRepository, EvidenceRepository>();
    services.AddScoped<IEvidenceHypothesisEvaluationRepository, EvidenceHypothesisEvaluationRepository>();
}
```

Call it in `CreateHostBuilder`:

```csharp
.ConfigureServices((context, services) =>
{
    RegisterExperimentConfigurations(services, context.Configuration);
    RegisterKernelServices(services);
    RegisterLogging(services, context.Configuration);
    RegisterDBServices(services, context.Configuration);  // ← Add this
});
```

### Step 2: Use in Code

```csharp
// Generate step execution ID
var stepExecutionId = Guid.NewGuid();

// Get repository
var hypothesisRepo = host.Services.GetRequiredService<IHypothesisRepository>();

// Save AI-generated hypotheses
var hypotheses = await ExecuteHypothesisBrainstormingAsync(host, config, input);
await hypothesisRepo.SaveBatchAsync(hypotheses, stepExecutionId);

// Later, retrieve them
var saved = await hypothesisRepo.GetByStepExecutionIdAsync(stepExecutionId);
```

### Step 3: Update Connection String

In `appsettings.json` or `appsettings.secrets.json`:

```json
{
  "ConnectionStrings": {
    "AchAiDBConnection": "Server=YOUR_SERVER;Database=ach-ai;Trusted_Connection=True;TrustServerCertificate=True;"
  }
}
```

## Common Patterns

### Pattern 1: Save Generated Data

```csharp
// AI generates
var hypotheses = await factory.ExecuteCoreAsync(input);

// Save to DB
var repo = host.Services.GetRequiredService<IHypothesisRepository>();
await repo.SaveBatchAsync(hypotheses, stepExecutionId);
```

### Pattern 2: Retrieve and Use

```csharp
// Get from DB
var repo = host.Services.GetRequiredService<IHypothesisRepository>();
var hypotheses = await repo.GetByStepExecutionIdAsync(stepExecutionId);

// Use as domain entities
DisplayHypotheses(hypotheses);
```

### Pattern 3: Complex Evaluation Workflow

```csharp
var stepExecutionId = Guid.NewGuid();

// 1. Save hypotheses
var hypothesisRepo = host.Services.GetRequiredService<IHypothesisRepository>();
await hypothesisRepo.SaveBatchAsync(hypotheses, stepExecutionId);

// 2. Save evidence
var evidenceRepo = host.Services.GetRequiredService<IEvidenceRepository>();
await evidenceRepo.SaveBatchAsync(evidenceList, stepExecutionId);

// 3. Build ID maps (needed for evaluations)
var savedHypotheses = await hypothesisRepo.GetByStepExecutionIdAsync(stepExecutionId);
var hypothesisIdMap = savedHypotheses.ToDictionary(h => h.ShortTitle, h => h.HypothesisId);

var savedEvidence = await evidenceRepo.GetByStepExecutionIdAsync(stepExecutionId);
var evidenceIdMap = savedEvidence.ToDictionary(e => e.Claim, e => e.EvidenceId);

// 4. Save evaluations (references hypotheses and evidence by ID)
var evaluationRepo = host.Services.GetRequiredService<IEvidenceHypothesisEvaluationRepository>();
await evaluationRepo.SaveBatchAsync(evaluations, stepExecutionId, hypothesisIdMap, evidenceIdMap);
```

## What You Get

✅ **No ambiguous references** - FrontendConsole only uses Domain.Entities
✅ **Clean Architecture** - Dependencies point inward
✅ **Simple mapping** - Just manual property assignment
✅ **Testable** - Easy to mock repositories
✅ **Type-safe** - Compiler catches mapping errors
✅ **Debuggable** - Step through mapping code
✅ **Maintainable** - Clear separation of concerns

## Trade-offs

| Aspect | Pro | Con |
|--------|-----|-----|
| **Simplicity** | Easy to understand | More code than AutoMapper |
| **Performance** | No reflection overhead | Have to manually update mappers |
| **Debugging** | Step through plain C# | N/A |
| **Dependencies** | Zero additional packages | Manual field mapping |

## When to Use What

| Scenario | Use This |
|----------|----------|
| Save AI results to DB | `repository.SaveBatchAsync()` |
| Load previous results | `repository.GetByStepExecutionIdAsync()` |
| Query specific items | `repository.GetByIdAsync()` |
| Filter by criteria | `repository.GetRefinedByStepExecutionIdAsync()` |
| Update DB records | `repository.MarkAsRefinedAsync()` |

## Troubleshooting

**Problem:** Can't resolve `IHypothesisRepository`
**Solution:** Make sure you called `RegisterDBServices()` in `CreateHostBuilder()`

**Problem:** Connection string error
**Solution:** Check `appsettings.secrets.json` has correct connection string

**Problem:** Evaluation save fails with "not found in ID map"
**Solution:** Save hypotheses and evidence BEFORE evaluations, then build ID maps

**Problem:** Mapper throws on unexpected null
**Solution:** Check domain entity has required fields populated by AI

## Next Steps

1. ✅ Review `/Mappers` - understand how conversion works
2. ✅ Review `/Repositories` - understand public API
3. ✅ Read `ARCHITECTURE.md` - understand design decisions
4. ✅ Read `USAGE_EXAMPLE.md` - see full integration example
5. ✅ Add `RegisterDBServices()` to `Program.cs`
6. ✅ Test with your AI workflows

That's it! You now have a clean, simple persistence layer that keeps your domain entities separate from your database models.
