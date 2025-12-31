using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using NIU.ACH_AI.Application.Configuration;
using NIU.ACH_AI.Application.DTOs;
using NIU.ACH_AI.Application.Interfaces;
using NIU.ACH_AI.FrontendConsole.Configuration;
using NIU.ACH_AI.FrontendConsole.Extensions;
using NIU.ACH_AI.FrontendConsole.Presentation;
using NIU.ACH_AI.Infrastructure;
using NIU.ACH_AI.Infrastructure.Persistence;

namespace NIU.ACH_AI.FrontendConsole
{
#pragma warning disable SKEXP0110 // Suppresses the warning about using Semantic Kernel for production purposes.

    public class Program
    {
        /// <summary>
        /// Entry point that starts the application and runs the ACH orchestration.
        /// </summary>
        static async Task Main(string[] args)
        {
            Console.WriteLine("=== Application started. Press Ctrl+C to shut down. ===");

            // Link up dependencies
            var host = CreateHostBuilder(args).Build();

            // Resolve Presenter from DI
            var consolePresenter = host.Services.GetRequiredService<ConsoleResultPresenter>();

            // Get the experiment settings
            var experimentSettings = host.Services.GetRequiredService<IOptions<ExperimentsSettings>>().Value;
            
            // Experiment Selection Strategy
            var experimentConfiguration = SelectExperiment(experimentSettings, args);

            if (experimentConfiguration == null)
            {
                Console.WriteLine("No valid experiment configuration found. Exiting application.");
                return;
            }

            // Display basic experiment information
            consolePresenter.DisplayExperimentInfo(experimentConfiguration);

            // Attempt to run the orchestration workflow
            try
            {
                var workflowResult = await RunOrchestrationAsync(host, experimentConfiguration);
                consolePresenter.DisplayWorkflowResult(workflowResult);
            }
            catch (OperationCanceledException)
            {
                Environment.ExitCode = 1;
                Console.Error.WriteLine("\nError: Operation canceled.");
            }
            catch (Exception ex)
            {
                Environment.ExitCode = 1;
                Console.Error.WriteLine($"\nError: {ex.Message}");
                Console.Error.WriteLine(ex);
            }
            finally
            {
                Console.WriteLine("\nPress any key to exit...");
                Console.ReadKey();
            }
        }

        /// <summary>
        /// Creates and configures the application host with services and configuration.
        /// </summary>
        public static IHostBuilder CreateHostBuilder(string[] args)
        {
            return Host.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration((context, config) =>
                {
                    config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
                    config.AddJsonFile($"appsettings.{context.HostingEnvironment.EnvironmentName}.json", optional: true, reloadOnChange: true);
                    config.AddJsonFile("appsettings.secrets.json", optional: true, reloadOnChange: true);
                    config.AddEnvironmentVariables();
                })
                .ConfigureServices((context, services) =>
                {
                    services.AddPersistence(context.Configuration);
                    services.AddFrontendServices(context.Configuration);
                });
        }

        #region Private Methods

        /// <summary>
        /// Selects an experiment configuration based on arguments or defaults to the first one.
        /// </summary>
        private static ExperimentConfiguration? SelectExperiment(ExperimentsSettings settings, string[] args)
        {
            if (settings?.Experiments == null || settings.Experiments.Length == 0)
            {
                Console.WriteLine("Error: No experiments configured in settings.");
                return null;
            }

            // Default to the first experiment if no arguments provided
            if (args == null || args.Length == 0)
            {
                Console.WriteLine($"No experiment specified. Defaulting to Experiment 1: {settings.Experiments[0].Name}");
                return settings.Experiments[0];
            }

            // Attempt to parse the first argument
            if (!int.TryParse(args[0], out int experimentNumber))
            {
                Console.WriteLine($"Error: Invalid experiment number format '{args[0]}'. Please provide an integer.");
                return null;
            }

            // Adjust for 1-based indexing (user sees 1, 2, 3...)
            int index = experimentNumber - 1;

            // Handle negative numbers, out of bounds
            if (index < 0 || index >= settings.Experiments.Length)
            {
                Console.WriteLine($"Error: Experiment number {experimentNumber} is out of bounds. Valid range is 1-{settings.Experiments.Length}.");
                return null;
            }

            Console.WriteLine($"Selected Experiment {experimentNumber}: {settings.Experiments[index].Name}");
            return settings.Experiments[index];
        }

        /// <summary>
        /// Runs the orchestration workflow for the specified experiment
        /// </summary>
        private static async Task<ACHWorkflowResult> RunOrchestrationAsync(
            IHost host, 
            ExperimentConfiguration experimentConfig)
        {
            // Validate the experiment configuration
            ExperimentConfigurationValidator.Validate(experimentConfig);

            // Get the workflow coordinator from DI container
            var workflowCoordinator = host.Services.GetRequiredService<IACHWorkflowCoordinator>();

            // Execute the complete workflow
            var workflowResult = await workflowCoordinator.ExecuteWorkflowAsync(experimentConfig);

            return workflowResult;
        }

        #endregion
    }

#pragma warning restore SKEXP0110
}
