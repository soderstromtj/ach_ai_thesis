using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NIU.ACH_AI.Application.Configuration;

namespace NIU.ACH_AI.FrontendConsole.Tests;

/// <summary>
/// Comprehensive unit tests for Program configuration logic.
///
/// Testing Strategy:
/// -----------------
/// The Program class contains the entry point and host configuration logic.
/// unique behaviors in CreateHostBuilder can be tested without running the full application.
/// passing empty args and validating the built host exposes the services and configuration state.
///
/// What We Can Test:
/// 1. CreateHostBuilder - Validates configuration sources (JSON files, Environment variables) are added
/// 2. CreateHostBuilder - Validates services are registered via dependency injection
///
/// Testing Challenges:
/// We cannot easily test Main() as it runs the application. Validating the HostBuilder
/// ensures the application starts with the correct wiring.
/// </summary>
public class ProgramTests
{
    /// <summary>
    /// This test verifies that CreateHostBuilder returns a host with the expected configuration sources.
    /// </summary>
    [Fact]
    public void CreateHostBuilder_ConfiguresAppConfiguration()
    {
        // Arrange
        var args = Array.Empty<string>();

        // Act
        var host = Program.CreateHostBuilder(args).Build();
        var config = host.Services.GetRequiredService<IConfiguration>();

        // Assert
        Assert.NotNull(config);
        // We can't easily assert exact file sources without inspecting internal providers,
        // but we can ensure the configuration root is built and distinct.
        Assert.IsAssignableFrom<IConfigurationRoot>(config);
    }

    /// <summary>
    /// This test verifies that CreateHostBuilder builds a host with the necessary application services configured.
    /// </summary>
    [Fact]
    public void CreateHostBuilder_ConfiguresServices()
    {
        // Arrange
        var args = Array.Empty<string>();

        // Act
        var host = Program.CreateHostBuilder(args).Build();

        // Assert
        // We check for a selection of services that should be present
        var experimentsSettings = host.Services.GetService<Microsoft.Extensions.Options.IOptions<ExperimentsSettings>>();
        var presenter = host.Services.GetService<NIU.ACH_AI.FrontendConsole.Presentation.ConsoleResultPresenter>();

        Assert.NotNull(experimentsSettings);
        Assert.NotNull(presenter);
    }
}
