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
                        return index;
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
                    config.AddJsonFile($"appsettings.{context.HostingEnvironment.EnvironmentName}.json", optional: false, reloadOnChange: true);
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
            var achStep = (ACHStep)achStepConfig.Id;

            // Build the input for the orchestration
            var input = new OrchestrationPromptInput
            {
                KeyQuestion = experimentConfig.KeyQuestion,
                Context = experimentConfig.Context,
                TaskInstructions = achStepConfig.TaskInstructions,
            };

            await ExecuteHypothesisBrainstormingAsync(host, achStepConfig, experimentConfig.GlobalAIServiceSettings, input);

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
        private static async Task ExecuteHypothesisBrainstormingAsync(
            IHost host, 
            ACHStepConfiguration stepConfiguration, 
            AIServiceSettings aiServiceSettings, 
            OrchestrationPromptInput input)
        {
            var loggerFactory = host.Services.GetRequiredService<ILoggerFactory>();
            var agentService = new AgentService(stepConfiguration.AgentConfigurations, aiServiceSettings, loggerFactory);
            var kernelBuilderService = host.Services.GetRequiredService<IKernelBuilderService>();
            var orchestrationOptions = Options.Create(stepConfiguration.OrchestrationSettings);

            var hypothesisFactory = new HypothesisBrainstormingOrchestrationFactory(
                agentService,
                kernelBuilderService,
                orchestrationOptions,
                loggerFactory);

            var hypotheses = await hypothesisFactory.ExecuteCoreAsync(input);
            DisplayHypotheses(hypotheses);
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
                Console.WriteLine($"Type: {item.Type}\tDescription: {item.Description}\n");
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

            // Map the AIServiceSettings section
            services.Configure<AIServiceSettings>(configuration.GetSection("AIServiceSettings"));

            // Register a post-configuration action to inject AIServiceSettings into each experiment
            services.PostConfigure<ExperimentsSettings>(settings =>
            {
                var aiSettings = configuration.GetSection("AIServiceSettings").Get<AIServiceSettings>();
                if (aiSettings != null && settings.Experiments != null)
                {
                    foreach (var experiment in settings.Experiments)
                    {
                        experiment.GlobalAIServiceSettings = aiSettings;
                    }
                }
            });
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
