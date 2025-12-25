using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NIU.ACH_AI.Application.Configuration;
using NIU.ACH_AI.Application.Interfaces;
using NIU.ACH_AI.Application.Services;
using NIU.ACH_AI.Domain.Entities;
using NIU.ACH_AI.FrontendConsole.Presentation;
using NIU.ACH_AI.Infrastructure.AI.Factories;
using NIU.ACH_AI.Infrastructure.AI.Services;
using NIU.ACH_AI.Infrastructure.Configuration;
using NIU.ACH_AI.Infrastructure.Persistence.Repositories;

namespace NIU.ACH_AI.FrontendConsole
{
#pragma warning disable SKEXP0110 // Suppresses the warning about using Semantic Kernel for production purposes.

    class Program
    {
        /// <summary>
        /// Entry point that starts the application and runs the ACH orchestration.
        /// </summary>
        static async Task Main(string[] args)
        {
            Console.WriteLine("=== Application started. Press Ctrl+C to shut down. ===");

            // Get a console presenter for displaying results
            var consolePresenter = new ConsoleResultPresenter();

            // Link up dependencies
            var host = CreateHostBuilder(args).Build();

            // Get the experiment settings
            var experimentSettings = host.Services.GetRequiredService<IOptions<ExperimentsSettings>>().Value;
            if (experimentSettings == null || experimentSettings.Experiments == null || experimentSettings.Experiments.Length == 0)
            {
                Console.WriteLine("Failed to retrieve experiment settings or no experiments configured. Exiting application.");
                return;
            }

            // For this example, just use the first experiment configuration
            ExperimentConfiguration experimentConfiguration = experimentSettings.Experiments[0];
            if (experimentConfiguration == null)
            {
                Console.WriteLine("Failed to retrieve experiment configuration or it doesn't exist. Exiting application.");
                return;
            }

            // Display basic experiment information
            consolePresenter.DisplayExperimentInfo(experimentConfiguration);

            // Attempt to run the orchestration workflow
            try
            {
                await RunOrchestrationAsync(host, experimentConfiguration, consolePresenter);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\nError: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
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
            var experimentIndex = ParseCommandLineArguments(args);

            return Host.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration((context, config) =>
                {
                    // Base configuration with non-sensitive defaults (committed to source control)
                    config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

                    // Environment-specific configuration (optional, can be committed or not)
                    config.AddJsonFile($"appsettings.{context.HostingEnvironment.EnvironmentName}.json",
                        optional: true, reloadOnChange: true);

                    // Secrets file - contains sensitive information (NOT committed to source control)
                    config.AddJsonFile("appsettings.secrets.json", optional: true, reloadOnChange: true);

                    // Environment variables can override all file-based configuration
                    config.AddEnvironmentVariables();
                })
                .ConfigureServices((context, services) =>
                {
                    RegisterExperimentConfigurations(services, context.Configuration);
                    RegisterKernelServices(services);
                    RegisterOrchestrationServices(services);
                    RegisterLogging(services, context.Configuration);
                    RegisterDBContext(context, services);
                });
        }

        /// <summary>
        /// Registers the Entity Framework database context in the service container.
        /// </summary>
        /// <param name="context">The HostBuilder context.</param>
        /// <param name="services">The service collection to register the context.</param>
        private static void RegisterDBContext(HostBuilderContext context, IServiceCollection services)
        {
            services.AddDbContext<Infrastructure.Persistence.Models.AchAIDbContext>(options =>
                                    options.UseSqlServer(context.Configuration.GetConnectionString("AchAiDBConnection")));

            // Register repositories
            services.AddScoped<IHypothesisRepository, HypothesisRepository>();
            services.AddScoped<IEvidenceRepository, EvidenceRepository>();
            services.AddScoped<IEvidenceHypothesisEvaluationRepository, EvidenceHypothesisEvaluationRepository>();
        }

        #region Private Methods
        /// <summary>
        /// Checks that the experiment configuration has all required fields.
        /// </summary>
        private static void ValidateExperimentConfiguration(ExperimentConfiguration config)
        {
            if (string.IsNullOrWhiteSpace(config.Id))
            {
                throw new InvalidOperationException(
                    "Id is not configured for this experiment. Please add 'Id' to the experiment settings in appsettings.json.");
            }

            if (string.IsNullOrWhiteSpace(config.Name))
            {
                throw new InvalidOperationException(
                    "Name is not configured for this experiment. Please add 'Name' to the experiment settings in appsettings.json.");
            }

            if (string.IsNullOrWhiteSpace(config.Description))
            {
                throw new InvalidOperationException(
                    "Description is not configured for this experiment. Please add 'Description' to the experiment settings in appsettings.json.");
            }

            if (config.ACHSteps == null || config.ACHSteps.Length == 0)
            {
                throw new InvalidOperationException(
                    "No ACH steps are configured for this experiment. Please add at least one ACH step to the experiment settings in appsettings.json.");
            }
        }

        /// <summary>
        /// Runs the orchestration workflow for the specified experiment
        /// </summary>
        /// <param name="host">The application host.</param>
        /// <param name="experimentConfig">The configuration for the experiment.</param>
        private static async Task RunOrchestrationAsync(IHost host, ExperimentConfiguration experimentConfig, ConsoleResultPresenter consoleResultPresenter)
        {
            // Validate the experiment configuration
            try
            {
                ValidateExperimentConfiguration(experimentConfig);
            }
            catch (Exception)
            {
                return;
            }

            // Get the workflow coordinator from DI container
            var workflowCoordinator = host.Services.GetRequiredService<IACHWorkflowCoordinator>();

            // Execute the complete workflow
            var workflowResult = await workflowCoordinator.ExecuteWorkflowAsync(experimentConfig);

            // Display results
            if (workflowResult.Hypotheses != null)
            {
                KeyQuestion = experimentConfig.KeyQuestion,
                Context = experimentConfig.Context,
                TaskInstructions = achStepConfig.TaskInstructions,
            };

            var hypotheses = await ExecuteHypothesisBrainstormingAsync(host, achStepConfig, input);
            consoleResultPresenter.DisplayHypotheses("Initial Hypotheses after Brainstorming Step:", hypotheses);

            if (workflowResult.RefinedHypotheses != null)
            {
                Hypotheses = hypotheses
            };

            // update the experiment configuration to the next ACH step
            achStepConfig = experimentConfig.ACHSteps[1];

            var refinedHypotheses = await ExecuteHypothesisEvaluationAsync(host, experimentConfig.ACHSteps[1], input);
            Console.WriteLine($"\nRefined Hypotheses after Evaluation Step:");
            DisplayHypotheses(refinedHypotheses);

            Console.WriteLine($"{new string('=', 70)}\n");

            // Update the experiment configuration to the next ACH step
            achStepConfig = experimentConfig.ACHSteps[2];

            var evidenceList = await ExecuteEvidenceExtractionAsync(host, experimentConfig.ACHSteps[2], input);
            DisplayEvidence(evidenceList);

            Console.WriteLine($"\n{new string('=', 70)}");

            // Loop through each evidence and hypothesis and evaluate their relationship
            foreach (var evidence in evidenceList)
            {
                foreach (var hypothesis in refinedHypotheses)
                {
                    Console.WriteLine($"Evaluating the following evidence and hypothesis:\nEvidence: {evidence.Claim}\nHypothesis: {hypothesis.HypothesisText}");

            if (workflowResult.Evaluations != null && workflowResult.Evaluations.Count > 0)
            {
                Console.WriteLine($"\nEvidence-Hypothesis Evaluations: {workflowResult.Evaluations.Count} total");
            }
        }

        /// <summary>
        /// Checks that the experiment configuration has all required fields.
        /// </summary>
        private static void ValidateExperimentConfiguration(ExperimentConfiguration config)
        {
            if (string.IsNullOrWhiteSpace(config.Id))
            {
                throw new InvalidOperationException(
                    "Id is not configured for this experiment. Please add 'Id' to the experiment settings in appsettings.json.");
            }

            if (string.IsNullOrWhiteSpace(config.Name))
            {
                throw new InvalidOperationException(
                    "Name is not configured for this experiment. Please add 'Name' to the experiment settings in appsettings.json.");
            }

            if (string.IsNullOrWhiteSpace(config.Description))
            {
                throw new InvalidOperationException(
                    "Description is not configured for this experiment. Please add 'Description' to the experiment settings in appsettings.json.");
            }

            if (config.ACHSteps == null || config.ACHSteps.Length == 0)
            {
                throw new InvalidOperationException(
                    "No ACH steps are configured for this experiment. Please add at least one ACH step to the experiment settings in appsettings.json.");
            }

        }

        /// <summary>
        /// Runs the hypothesis brainstorming step and displays the results.
        /// </summary>
        private static async Task<List<Hypothesis>> ExecuteHypothesisBrainstormingAsync(
            IHost host,
            ACHStepConfiguration stepConfiguration,
            OrchestrationPromptInput input)
        {
            var loggerFactory = host.Services.GetRequiredService<ILoggerFactory>();
            var aiServiceSettings = host.Services.GetRequiredService<IOptions<AIServiceSettings>>().Value;
            var agentService = new AgentService(stepConfiguration.AgentConfigurations, aiServiceSettings, loggerFactory);
            var kernelBuilderService = host.Services.GetRequiredService<IKernelBuilderService>();
            var orchestrationOptions = Options.Create(stepConfiguration.OrchestrationSettings);

            var hypothesisFactory = new HypothesisBrainstormingOrchestrationFactory(
                agentService,
                kernelBuilderService,
                orchestrationOptions,
                loggerFactory);

            var hypotheses = await hypothesisFactory.ExecuteCoreAsync(input);

            return hypotheses;
        }

        }

        /// <summary>
        /// Prints the generated hypotheses to the console.
        /// </summary>
        private static void DisplayHypotheses(List<Hypothesis> hypotheses)
        {
            Console.WriteLine($"\n{new string('=', 70)}");
            Console.WriteLine("\nGenerated Hypotheses:");

            int hypothesisNum = 1;
            foreach (var hypothesis in hypotheses)
            {
                Console.WriteLine($"\n{hypothesisNum}. {hypothesis.ShortTitle}");
                Console.WriteLine($"Hypothesis: {hypothesis.HypothesisText}");
                hypothesisNum++;
            }

        }

        /// <summary>
        /// Prints the extracted evidence to the console.
        /// </summary>
        private static void DisplayEvidence(List<Evidence> evidence)
        {
            Console.WriteLine($"\n{new string('=', 70)}");
            Console.WriteLine("\nExtracted Evidence:");

            foreach (var item in evidence)
            {
                Console.WriteLine(item.ToString());
                Console.WriteLine();
                Console.WriteLine(new string('-', 40));
                Console.WriteLine();

            }
        }

        /// <summary>
        /// Extracts the ACH step number and experiment index from command-line arguments.
        /// </summary>
        private static int? ParseCommandLineArguments(string[] args)
        {
            int? experimentIndex = null;

            if (args.Length >= 1 && int.TryParse(args[0], out int stepNum))
            {
                experimentIndex = stepNum;
                return experimentIndex;
            }

            return 0;
        }

        private static void RegisterExperimentConfigurations(IServiceCollection services, IConfiguration configuration)
        {
            // Map the root configuration to ExperimentsSettings (which has Experiments[] property)
            services.Configure<ExperimentsSettings>(configuration);

            // Map the AIServiceSettings section - kept separate and injected where needed
            services.Configure<AIServiceSettings>(configuration.GetSection("AIServiceSettings"));
        }

        /// <summary>
        /// Registers Semantic Kernel services and AI adapters in the container.
        /// </summary>
        private static void RegisterKernelServices(IServiceCollection services)
        {
            // KernelBuilderService builds a default kernel for orchestration (e.g., structured output)
            services.AddSingleton<IKernelBuilderService, KernelBuilderService>();
        }

        /// <summary>
        /// Registers orchestration and workflow services in the container.
        /// </summary>
        private static void RegisterOrchestrationServices(IServiceCollection services)
        {
            // Register orchestration execution service
            services.AddSingleton<IOrchestrationExecutor, OrchestrationExecutor>();

            // Register factory provider for creating orchestration factories
            services.AddSingleton<IOrchestrationFactoryProvider, OrchestrationFactoryProvider>();

            // Register workflow coordinator for managing ACH workflow execution
            services.AddScoped<IACHWorkflowCoordinator, ACHWorkflowCoordinator>();
        }

        /// <summary>
        /// Sets up logging providers and configuration for the application.
        /// </summary>
        private static void RegisterLogging(IServiceCollection services, IConfiguration configuration)
        {
            // Configure logging to use Console and Debug providers
            services.AddLogging(builder =>
            {
                builder.AddConsole();
                builder.AddDebug();
                builder.AddConfiguration(configuration.GetSection("Logging"));
            });
        }

        #endregion
    }
}

#pragma warning restore SKEXP0110 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
