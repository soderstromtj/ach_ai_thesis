using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SemanticKernelPractice.Configuration;
using SemanticKernelPractice.Factories;
using SemanticKernelPractice.Models;
using SemanticKernelPractice.Services;
using System;

namespace SemanticKernelPractice
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

            // Link up dependencies
            var host = CreateHostBuilder(args).Build();

            // Get the experiment settings
            var experimentSettings = host.Services.GetRequiredService<IOptions<ExperimentsSettings>>().Value;
            if (experimentSettings == null || experimentSettings.Experiments == null || experimentSettings.Experiments.Length == 0)
            {
                Console.WriteLine("Failed to retrieve experiment settings or no experiments configured. Exiting application.");
                return;
            }

            // Extract the experiment number from args
            int experimentIndex = ExtractExperimentIndexFromArgs(args);
            ExperimentConfiguration experimentConfiguration = experimentSettings.Experiments[experimentIndex];
            if (experimentConfiguration == null)
            {
                Console.WriteLine("Failed to retrieve experiment configuration or it doesn't exist. Exiting application.");
                return;
            }
                        
            Console.WriteLine($"Experiment {experimentSettings.Experiments[experimentIndex].Id}: {experimentSettings.Experiments[experimentIndex].Description}");
            Console.WriteLine(new string('=', 70));

            // Attempt to run the orchestration workflow
            try
            {
                await RunOrchestrationAsync(host, experimentConfiguration);
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

        private static int ExtractExperimentIndexFromArgs(string[] args)
        {
            if (args.Length > 0)
            {
                var rawArg = args[0]?.Trim();

                if (string.IsNullOrEmpty(rawArg))
                {
                    Console.WriteLine($"Warning: Invalid argument, using default 0.");
                    return 0;
                }
                else if (rawArg.StartsWith('-'))
                {
                    Console.WriteLine($"Warning: Negative experiment index '{rawArg}' is not allowed. Using default 0.");
                    return 0;
                }
                else
                {
                    try
                    {
                        int index = int.Parse(rawArg);
                        return index - 1;
                    }
                    catch (Exception)
                    {
                        Console.WriteLine($"Warning: Unable to parse experiment index '{rawArg}'. Using default 0.");
                        return 0;
                    }
                }
            }

            return 0;
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
                });
        }

        #region Private Methods
        /// <summary>
        /// Runs the orchestration workflow for the specified experiment
        /// </summary>
        private static async Task RunOrchestrationAsync(IHost host, ExperimentConfiguration experimentConfig)
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

            // Build a logger factory to pass into each of the agent orchestration services
            var loggerFactory = host.Services.GetRequiredService<ILoggerFactory>();

            // Run the first ACH step for this example - Hypothesis Brainstorming
            Console.WriteLine($"\n{new string('=', 70)}");
            var achStepConfig = experimentConfig.ACHSteps[0];

            // Build the input for the orchestration
            var input = new OrchestrationPromptInput
            {
                KeyQuestion = experimentConfig.KeyQuestion,
                Context = experimentConfig.Context,
                TaskInstructions = achStepConfig.TaskInstructions,
            };

            var hypotheses = await ExecuteHypothesisBrainstormingAsync(host, achStepConfig, input);
            Console.WriteLine($"\nInitial Hypotheses after Brainstorming Step:");
            DisplayHypotheses(hypotheses);

            // Update the input with the generated hypotheses for the next step
            input.HypothesisResult = new HypothesisResult
            {
                Hypotheses = hypotheses
            };

            // update the experiment configuration to the next ACH step
            achStepConfig = experimentConfig.ACHSteps[1];

            var refinedHypotheses = await ExecuteHypothesisEvaluationAsync(host, experimentConfig.ACHSteps[1], input);
            Console.WriteLine($"\nRefined Hypotheses after Evaluation Step:");
            DisplayHypotheses(refinedHypotheses);

            Console.WriteLine($"{new string('=', 70)}\n");
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

        /// <summary>
        /// Runs the hypothesis evaluation and refinement step and returns a refined list of hypotheses.
        /// </summary>
        private static async Task<List<Hypothesis>> ExecuteHypothesisEvaluationAsync(IHost host, ACHStepConfiguration stepConfiguration, OrchestrationPromptInput input)
        {
            var loggerFactory = host.Services.GetRequiredService<ILoggerFactory>();
            var aiServiceSettings = host.Services.GetRequiredService<IOptions<AIServiceSettings>>().Value;
            var agentService = new AgentService(stepConfiguration.AgentConfigurations, aiServiceSettings, loggerFactory);
            var kernelBuilderService = host.Services.GetRequiredService<IKernelBuilderService>();
            var orchestrationOptions = Options.Create(stepConfiguration.OrchestrationSettings);

            var hypothesisEvaluationFactory = new HypothesisRefinementOrchestrationFactory(
                agentService,
                kernelBuilderService,
                orchestrationOptions,
                loggerFactory);

            var refinedHypotheses = await hypothesisEvaluationFactory.ExecuteCoreAsync(input);

            return refinedHypotheses;
        }

        /// <summary>
        /// Runs the evidence extraction step and displays the results.
        /// </summary>
        private static async Task ExecuteEvidenceExtractionAsync(object factory, OrchestrationPromptInput input)
        {
            var evidenceFactory = factory as IOrchestrationFactory<List<Evidence>>
                ?? throw new InvalidOperationException("Factory is not of the expected type for Evidence Extraction.");

            var evidence = await evidenceFactory.ExecuteCoreAsync(input);
            DisplayEvidence(evidence);
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
                Console.WriteLine($"\n{hypothesisNum}. {hypothesis.Title}");
                Console.WriteLine($"   Rationale: {hypothesis.Rationale}");
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

            // Note: AgentService is constructed manually per ACH step with step-specific configurations
            // See ExecuteHypothesisBrainstormingAsync for usage

            services.AddSingleton<ConsoleFormatter>();
        }

        /// <summary>
        /// Registers orchestration factories for each ACH step in the container.
        /// Note: Factories are currently created manually per ACH step with step-specific configurations.
        /// See ExecuteHypothesisBrainstormingAsync for example usage.
        /// </summary>
        private static void RegisterOrchestrationServices(IServiceCollection services)
        {
            // No registrations needed - factories are created manually with step-specific configurations
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
