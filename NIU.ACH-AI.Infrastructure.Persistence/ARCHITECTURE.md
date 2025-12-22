# Repository Pattern Architecture

## Overview

This architecture resolves the ambiguous reference problem while maintaining Clean Architecture principles through a **Repository Pattern** with **Manual Mapping**.

## Layer Structure

```
┌─────────────────────────────────────────────────────────┐
│ FrontendConsole (Presentation)                          │
│ - Only imports NIU.ACH_AI.Domain.Entities               │
│ - Uses IHypothesisRepository, IEvidenceRepository, etc. │
│ - Never sees database models                            │
└─────────────────────────────────────────────────────────┘
                          ↓
┌─────────────────────────────────────────────────────────┐
│ Domain (Core Business Logic)                            │
│ - Entities: Hypothesis, Evidence, etc.                  │
│ - ValueObjects: EvidenceType, EvaluationScore           │
│ - No dependencies                                       │
└─────────────────────────────────────────────────────────┘
                          ↑
┌─────────────────────────────────────────────────────────┐
│ Infrastructure.Persistence (Data Access)                │
│                                                         │
│ ┌─────────────────────────────────────────────────┐   │
│ │ Repositories (Public Interface)                 │   │
│ │ - IHypothesisRepository → HypothesisRepository  │   │
│ │ - IEvidenceRepository → EvidenceRepository      │   │
│ │ - Works only with Domain.Entities               │   │
│ └─────────────────────────────────────────────────┘   │
│                         ↓                               │
│ ┌─────────────────────────────────────────────────┐   │
│ │ Mappers (Internal)                              │   │
│ │ - HypothesisMapper (Domain ↔ DB)               │   │
│ │ - EvidenceMapper (Domain ↔ DB)                 │   │
│ │ - Converts between layers                       │   │
│ └─────────────────────────────────────────────────┘   │
│                         ↓                               │
│ ┌─────────────────────────────────────────────────┐   │
│ │ Models (EF Scaffolded)                          │   │
│ │ - Persistence.Models.Hypothesis                 │   │
│ │ - Persistence.Models.Evidence                   │   │
│ │ - Only used internally                          │   │
│ └─────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────┘
                          ↓
┌─────────────────────────────────────────────────────────┐
│ Database (SQL Server)                                   │
└─────────────────────────────────────────────────────────┘
```

## Key Design Decisions

### 1. **Repository Interfaces Use Domain Entities**

```csharp
// ✅ GOOD: Interface uses domain types
public interface IHypothesisRepository
{
    Task SaveBatchAsync(IEnumerable<Hypothesis> hypotheses, Guid stepExecutionId);
    Task<List<Hypothesis>> GetByStepExecutionIdAsync(Guid stepExecutionId);
}

// ❌ BAD: Would expose database models
public interface IHypothesisRepository
{
    Task SaveBatchAsync(IEnumerable<Models.Hypothesis> hypotheses, Guid stepExecutionId);
}
```

**Why?** This keeps the presentation layer decoupled from database implementation details.

### 2. **Mappers are Static Classes**

```csharp
public static class HypothesisMapper
{
    public static Models.Hypothesis ToDatabase(Domain.Entities.Hypothesis domain, Guid stepExecutionId)
    public static Domain.Entities.Hypothesis ToDomain(Models.Hypothesis database)
}
```

**Why?**
- Simple and fast (no reflection)
- No state to manage
- Easy to unit test
- Clear, explicit mapping

### 3. **Manual Mapping (Not AutoMapper/Mapperly)**

**Pros:**
- ✅ No additional dependencies
- ✅ Explicit and debuggable
- ✅ Compiler checks field mappings
- ✅ Zero runtime overhead

**Cons:**
- ❌ More code to write
- ❌ Must manually update when fields change

**Trade-off:** We chose simplicity and explicitness over brevity.

### 4. **ID Maps for Complex Relationships**

When saving evaluations, we need to know which database IDs correspond to which hypotheses/evidence:

```csharp
var hypothesisIdMap = new Dictionary<string, Guid>
{
    ["Climate change theory"] = Guid.Parse("..."),
    ["Economic factors theory"] = Guid.Parse("...")
};

await evaluationRepo.SaveBatchAsync(evaluations, stepExecutionId, hypothesisIdMap, evidenceIdMap);
```

**Why?** Domain entities don't have database IDs when AI generates them. We use business keys (ShortTitle, Claim) to correlate them after persistence.

### 5. **AsNoTracking() for Queries**

```csharp
var dbEntities = await _context.Hypotheses
    .AsNoTracking()  // ← Important!
    .ToListAsync();
```

**Why?** We immediately convert to domain entities, so we don't need EF change tracking. This improves performance.

## Comparison: Before vs After

### Before (Ambiguous References)

```csharp
using NIU.ACH_AI.Domain.Entities;
using NIU.ACH_AI.Infrastructure.Persistence.Models;

// ❌ ERROR: 'Hypothesis' is ambiguous!
List<Hypothesis> hypotheses = ...;
```

### After (Clean Separation)

```csharp
using NIU.ACH_AI.Domain.Entities;
// No need to import Persistence.Models!

// ✅ Unambiguous: always Domain.Entities.Hypothesis
List<Hypothesis> hypotheses = await hypothesisFactory.ExecuteCoreAsync(input);

// ✅ Save to database via repository
var repo = host.Services.GetRequiredService<IHypothesisRepository>();
await repo.SaveBatchAsync(hypotheses, stepExecutionId);
```

## Testing Strategy

### Unit Testing Mappers

```csharp
[Fact]
public void HypothesisMapper_ToDatabase_MapsFieldsCorrectly()
{
    // Arrange
    var domain = new Domain.Entities.Hypothesis
    {
        ShortTitle = "Test",
        HypothesisText = "Test text"
    };
    var stepExecutionId = Guid.NewGuid();

    // Act
    var db = HypothesisMapper.ToDatabase(domain, stepExecutionId);

    // Assert
    Assert.Equal("Test", db.ShortTitle);
    Assert.Equal("Test text", db.HypothesisText);
    Assert.Equal(stepExecutionId, db.StepExecutionId);
    Assert.NotEqual(Guid.Empty, db.HypothesisId);
}
```

### Integration Testing Repositories

```csharp
[Fact]
public async Task HypothesisRepository_SaveAndRetrieve_RoundTrip()
{
    // Arrange
    var options = new DbContextOptionsBuilder<AchAIDbContext>()
        .UseInMemoryDatabase(databaseName: "TestDb")
        .Options;

    using var context = new AchAIDbContext(options);
    var repo = new HypothesisRepository(context);

    var hypotheses = new List<Hypothesis>
    {
        new() { ShortTitle = "H1", HypothesisText = "Text 1" },
        new() { ShortTitle = "H2", HypothesisText = "Text 2" }
    };
    var stepExecutionId = Guid.NewGuid();

    // Act
    await repo.SaveBatchAsync(hypotheses, stepExecutionId);
    var retrieved = await repo.GetByStepExecutionIdAsync(stepExecutionId);

    // Assert
    Assert.Equal(2, retrieved.Count);
    Assert.Contains(retrieved, h => h.ShortTitle == "H1");
    Assert.Contains(retrieved, h => h.ShortTitle == "H2");
}
```

## Migration Path

If you later want to add AutoMapper or Mapperly:

1. Keep repository interfaces unchanged
2. Replace mapper implementations
3. No changes needed in FrontendConsole

This is the benefit of abstraction!

## Summary

| Aspect | Solution |
|--------|----------|
| **Problem** | Ambiguous references between Domain and Persistence entities |
| **Solution** | Repository Pattern with Manual Mapping |
| **Complexity** | Low (just mappers + repositories) |
| **Performance** | Excellent (no reflection) |
| **Maintainability** | Good (explicit, easy to debug) |
| **Clean Architecture** | ✅ Preserved |
| **Testing** | ✅ Easy to mock and test |

**Next Steps:**
1. Review the mappers in `/Mappers`
2. Review the repository interfaces in `/Repositories`
3. See `USAGE_EXAMPLE.md` for integration examples
4. Add to `Program.cs` when ready to persist data
