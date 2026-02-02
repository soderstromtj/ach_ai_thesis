# Dependency Inversion Principle (DIP) Analysis Report

## 1. Executive Summary

This report analyzes the `NIU.ACH-AI` codebase for violations of the **Dependency Inversion Principle (DIP)** and related architectural code smells. The analysis focuses on code-level violations (high-level modules depending on low-level modules) and architectural coupling.

**Overall Status:** The project follows a Clean Architecture structure with proper project-level dependency separation (`Domain` <- `Application` <- `Infrastructure`). However, within the `Infrastructure` layer, there are several instances of tight coupling where "manager" or "executor" services directly instantiate their dependencies or use concrete implementations instead of abstractions.

## 2. Summary of Findings

| File | Violation Type | Description | Recommendation |
| :--- | :--- | :--- | :--- |
| `AgentService.cs` | **Direct Instantiation** | Methods directly instantiate concrete `*KernelAdapter` classes (e.g., `new OpenAIKernelAdapter(...)`). | Inject an `IKernelAdapterFactory` or a strategy collection of `IKernelBuilderAdapter`. |
| `OrchestrationExecutor.cs` | **Direct Instantiation** | The `CreateAgentService` method manually instantiates the concrete `AgentService` class using `new`. | Inject an `IAgentServiceFactory` or `Func<ACHStepConfiguration, IAgentService>`. |
| `OrchestrationFactoryProvider.cs` | **Service Locator / Rigid Factory** | Uses `Activator.CreateInstance` to create concrete factory types based on hardcoded string matching. | Register factories in DI (e.g., Keyed Services) and resolve them via `IServiceProvider` or a delegate factory. |
| `ACHWorkflowCoordinator.cs` | **Implicit Dependency** | Uses `DateTime.UtcNow` directly, creating a hard dependency on the system clock. | Inject `TimeProvider` (or `ITimeProvider`) to allow testing time-dependent logic. |

## 3. Detailed Analysis

### 3.1. AgentService.cs

**File:** `NIU.ACH-AI.Infrastructure/AI/Services/AgentService.cs`

**Issue:**
The `BuildKernelForAgent` method contains a switch statement that directly instantiates concrete adapter classes using the `new` keyword.

```csharp
IKernelBuilderAdapter adapter = effectiveServiceId switch
{
    "openai" => CreateOpenAIAdapter(), // Calls new OpenAIKernelAdapter(...)
    "azure" => CreateAzureOpenAIAdapter(), // Calls new AzureOpenAIKernelAdapter(...)
    "ollama" => CreateOllamaAdapter(), // Calls new OllamaKernelAdapter(...)
    _ => throw new InvalidOperationException(...)
};
```

**Why this is a violation:**
`AgentService` (a high-level orchestration of agents) depends directly on the concrete implementation of specific AI providers (Low-level details). If you wanted to test `AgentService` without real AI connections, you cannot easily mock these adapters because they are hard-coded.

**Recommendation:**
Refactor to use a **Strategy Pattern** or **Factory Pattern**. Register all `IKernelBuilderAdapter` implementations in the Dependency Injection container and inject `IEnumerable<IKernelBuilderAdapter>` into `AgentService`. Select the correct adapter based on the `SupportedProvider` property.

---

### 3.2. OrchestrationExecutor.cs

**File:** `NIU.ACH-AI.Infrastructure/AI/Services/OrchestrationExecutor.cs`

**Issue:**
The `CreateAgentService` method creates a new instance of `AgentService` directly.

```csharp
public IAgentService CreateAgentService(ACHStepConfiguration stepConfiguration)
{
    return new AgentService(
        stepConfiguration.AgentConfigurations,
        _aiServiceSettings,
        _loggerFactory,
        _httpClientFactory,
        _agentConfigurationPersistence);
}
```

**Why this is a violation:**
`OrchestrationExecutor` is tightly coupled to the concrete `AgentService` class. It also acts as a "Middleman" for dependencies (injecting `_httpClientFactory`, etc., just to pass them to `AgentService`), which clutters its own constructor.

**Recommendation:**
Introduce a factory interface `IAgentServiceFactory`:
```csharp
public interface IAgentServiceFactory
{
    IAgentService Create(ACHStepConfiguration configuration);
}
```
Inject this factory into `OrchestrationExecutor` and let the DI container (or the factory implementation) handle the resolution of `AgentService` dependencies.

---

### 3.3. OrchestrationFactoryProvider.cs

**File:** `NIU.ACH-AI.Infrastructure/AI/Factories/OrchestrationFactoryProvider.cs`

**Issue:**
The provider uses `Activator.CreateInstance` to manually construct concrete orchestration factory classes (`HypothesisBrainstormingOrchestrationFactory`, etc.).

```csharp
var factory = (TFactory)Activator.CreateInstance(
    typeof(TFactory),
    agentService,
    kernelBuilderService,
    orchestrationOptions,
    loggerFactory,
    agentResponsePersistence)!;
```

**Why this is a violation:**
This code mimics a Service Locator and relies on the concrete types having a specific constructor signature. It bypasses the Dependency Injection container, meaning if one of the factories requires a new dependency, you must modify `OrchestrationFactoryProvider` to pass it manually. This violates the **Open/Closed Principle**.

**Recommendation:**
Register the specific orchestration factories in the DI container. Since the selection depends on a runtime string (the step name), use **Keyed Services** (available in .NET 8+) or a **Factory Delegate**:

```csharp
// In DI Setup
services.AddKeyedTransient<IOrchestrationFactory<List<Hypothesis>>, HypothesisBrainstormingOrchestrationFactory>("hypothesis brainstorming");

// In Provider
public IOrchestrationFactory<TResult> CreateFactory<TResult>(ACHStepConfiguration config)
{
    return _serviceProvider.GetRequiredKeyedService<IOrchestrationFactory<TResult>>(config.Name.ToLowerInvariant());
}
```

---

### 3.4. ACHWorkflowCoordinator.cs

**File:** `NIU.ACH-AI.Application/Services/ACHWorkflowCoordinator.cs`

**Issue:**
Direct usage of `DateTime.UtcNow`.

```csharp
Timestamp = DateTime.UtcNow
```

**Why this is a violation:**
While minor, this creates an implicit dependency on the system clock, making it difficult to write deterministic unit tests for time-sensitive logic (e.g., verifying timeouts or timestamps).

**Recommendation:**
Inject `TimeProvider` (standard in .NET 8/9) or a custom `IDateTimeProvider` interface.

## 4. Conclusion

The application architecture is generally sound, adhering to the project structure of Clean Architecture. The identified violations are primarily "internal coupling" within the `Infrastructure` layer. Refactoring these using **Abstract Factories** and **Dependency Injection** capabilities (like Keyed Services) will significantly improve testability and maintainability.
