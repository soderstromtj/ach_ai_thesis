using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NIU.ACH_AI.Application.Configuration;
using NIU.ACH_AI.Application.Interfaces;
using NIU.ACH_AI.FrontendConsole.Extensions;
using NIU.ACH_AI.FrontendConsole.Presentation;
using NIU.ACH_AI.Infrastructure.Configuration;

namespace NIU.ACH_AI.FrontendConsole.Tests;

/// <summary>
/// Comprehensive unit tests for DependencyInjection.
///
/// Testing Strategy:
/// -----------------
/// DependencyInjection is an extension class that registers services into the IServiceCollection.
/// To test it, we create a fresh ServiceCollection, Mock configuration, run the registration,
/// and then verify that the expected services are present in the collection with the correct lifetimes.
///
/// What We Can Test:
/// 1. AddFrontendServices - Verifies configuration objects are registered
/// 2. AddFrontendServices - Verifies core services are registered (KernelBuilder, OrchestrationExecutor, etc.)
/// 3. AddFrontendServices - Verifies presentation layer services (ConsoleResultPresenter)
/// 4. AddFrontendServices - Verifies logging is configured
///
/// Testing Challenges:
/// Resolving services (BuildServiceProvider) can trigger internal constructors or logic we don't want to run.
/// Ideally, we check the IServiceCollection descriptors directly to see if the types are registered.
/// </summary>
public class DependencyInjectionTests
{
    private readonly IConfiguration _configuration;

    public DependencyInjectionTests()
    {
        var inMemorySettings = new Dictionary<string, string>
        {
            {"Experiments:0:Id", "TestExp"},
            {"Logging:LogLevel:Default", "Information"},
            {"AIServiceSettings:OpenAI:ApiKey", "test-key"} // minimal config to satisfy loose requirements if any
        };

        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings!)
            .Build();
    }

    /// <summary>
    /// This test verifies that AddFrontendServices registers the IOptions objects for configuration settings.
    /// </summary>
    [Fact]
    public void AddFrontendServices_RegistersConfigurationSettings()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddFrontendServices(_configuration);
        var provider = services.BuildServiceProvider();

        // Assert
        var experimentsSettings = provider.GetService<IOptions<ExperimentsSettings>>();
        var aiSettings = provider.GetService<IOptions<AIServiceSettings>>();

        Assert.NotNull(experimentsSettings);
        Assert.NotNull(aiSettings);
    }

    /// <summary>
    /// This test verifies that AddFrontendServices registers the core application and infrastructure services.
    /// </summary>
    [Fact]
    public void AddFrontendServices_RegistersCoreServices()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddFrontendServices(_configuration);

        // Assert - Check ServiceCollection directly to avoid resolving complex dependencies
        Assert.Contains(services, s => s.ServiceType == typeof(IKernelBuilderService) && s.Lifetime == ServiceLifetime.Singleton);
        Assert.Contains(services, s => s.ServiceType == typeof(IOrchestrationExecutor) && s.Lifetime == ServiceLifetime.Singleton);
        Assert.Contains(services, s => s.ServiceType == typeof(IOrchestrationFactoryProvider) && s.Lifetime == ServiceLifetime.Singleton);
        Assert.Contains(services, s => s.ServiceType == typeof(ITokenUsageExtractor) && s.Lifetime == ServiceLifetime.Singleton);
        Assert.Contains(services, s => s.ServiceType == typeof(IACHWorkflowCoordinator) && s.Lifetime == ServiceLifetime.Scoped);
    }

    /// <summary>
    /// This test verifies that AddFrontendServices registers the console presentation services.
    /// </summary>
    [Fact]
    public void AddFrontendServices_RegistersPresentationServices()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddFrontendServices(_configuration);

        // Assert
        Assert.Contains(services, s => s.ServiceType == typeof(ConsoleResultPresenter) && s.Lifetime == ServiceLifetime.Transient);
    }

    /// <summary>
    /// This test verifies that AddFrontendServices returns the service collection for chaining.
    /// </summary>
    [Fact]
    public void AddFrontendServices_ReturnsServiceCollection()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var result = services.AddFrontendServices(_configuration);

        // Assert
        Assert.Same(services, result);
    }
}
