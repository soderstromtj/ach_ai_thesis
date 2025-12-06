using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SemanticKernelPractice.Configuration;
using SemanticKernelPractice.Factories;
using SemanticKernelPractice.Models;
using SemanticKernelPractice.Services;

namespace SemanticKernelPractice
{
#pragma warning disable SKEXP0110 // Suppresses the warning about using Semantic Kernel for production purposes.

    class Program
    {
        private static class Constants
        {
            public const string DefaultAchStepEnvironmentVariable = "ACH_STEP";
            public const string DefaultExperimentName = "Baseline";
            public const string ExperimentNameEnvironmentVariable = "ACH_EXPERIMENT_NAME";
            public const string AchStepPrefix = "ACHStep";
            public const string DefaultAchStep = "ACHStep1";
            public const int SeparatorLength = 70;
        }

        /// <summary>
        /// Entry point that starts the application and runs the ACH orchestration.
        /// </summary>
        static async Task Main(string[] args)
        {
            var host = CreateHostBuilder(args).Build();

            Console.WriteLine("=== Application started. Press Ctrl+C to shut down. ===");

            if (args.Length >= 1)
            {
                Console.WriteLine($"Command-line args: ACH Step {args[0]}");
            }

            var experimentConfig = host.Services.GetRequiredService<ExperimentConfiguration>();
            Console.WriteLine($"Experiment: {experimentConfig.Name}");
            Console.WriteLine($"AI Provider: Unified (Multi-Provider support enabled)\n");

            var achStep = ParseAchStepFromArgs(args);
            Console.WriteLine($"Task: Running ACH {achStep} (Step {(int)achStep}).\n");
            Console.WriteLine(new string('=', Constants.SeparatorLength));

            try
            {
                await RunOrchestrationAsync(host, experimentConfig, achStep);
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
            var (achStepNumber, experimentIndex) = ParseCommandLineArguments(args);

            return Host.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration((context, config) =>
                {
                    config.AddJsonFile($"appsettings.{context.HostingEnvironment.EnvironmentName}.json", optional: false, reloadOnChange: true);
                    config.AddEnvironmentVariables();
                })
                .ConfigureServices((context, services) =>
                {
                    RegisterConfiguration(services, context.Configuration, achStepNumber, experimentIndex);
                    RegisterKernelServices(services);
                    RegisterOrchestrationServices(services);
                    RegisterLogging(services, context.Configuration);
                });
        }

        #region Private Methods
        /// <summary>
        /// Gets the ACH step number from command-line arguments.
        /// </summary>
        private static ACHStep ParseAchStepFromArgs(string[] args)
        {
            if (args.Length >= 1 && int.TryParse(args[0], out int stepNum))
            {
                return (ACHStep)stepNum;
            }

            return ParseAchStepFromEnvironment();
        }

        /// <summary>
        /// Gets the ACH step from environment variables or uses the default.
        /// </summary>
        private static ACHStep ParseAchStepFromEnvironment()
        {
            var achStepEnv = Environment.GetEnvironmentVariable(Constants.DefaultAchStepEnvironmentVariable) ?? Constants.DefaultAchStep;

            if (achStepEnv.StartsWith(Constants.AchStepPrefix) && int.TryParse(achStepEnv.Substring(Constants.AchStepPrefix.Length), out int envStepNum))
            {
                return (ACHStep)envStepNum;
            }

            return ACHStep.HypothesisGeneration; // Default to step 1
        }

        /// <summary>
        /// Runs the orchestration workflow for the specified ACH step.
        /// </summary>
        private static async Task RunOrchestrationAsync(IHost host, ExperimentConfiguration experimentConfig, ACHStep achStep)
        {
            var factorySelector = host.Services.GetRequiredService<IOrchestrationFactorySelector>();
            var factory = factorySelector.GetFactory(achStep);

            ValidateExperimentConfiguration(experimentConfig);

            var input = new OrchestrationPromptInput()
            {
                KeyQuestion = experimentConfig.KeyIntelligenceQuestion,
                Context = experimentConfig.Context,
                TaskInstructions = experimentConfig.TaskInstructions,
                HypothesisResult = null,
                EvidenceResult = null
            };

            Console.WriteLine($"\nExecuting {achStep} Orchestration...\n");

            await ExecuteOrchestrationAsync(factory, achStep, input);

            Console.WriteLine($"{new string('=', Constants.SeparatorLength)}\n");
        }

        /// <summary>
        /// Checks that the experiment configuration has all required fields.
        /// </summary>
        private static void ValidateExperimentConfiguration(ExperimentConfiguration config)
        {
            if (string.IsNullOrWhiteSpace(config.Context))
            {
                throw new InvalidOperationException(
                    "Context is not configured for this experiment. Please add 'Context' to the experiment settings in appsettings.json.");
            }

            if (string.IsNullOrWhiteSpace(config.KeyIntelligenceQuestion))
            {
                throw new InvalidOperationException(
                    "KIQ is not configured for this experiment. Please add 'KeyIntelligenceQuestion' to the experiment settings in appsettings.json.");
            }

            if (string.IsNullOrWhiteSpace(config.TaskInstructions))
            {
                throw new InvalidOperationException(
                    "Task instructions are not configured for this experiment. Please add 'TaskInstructions' to the experiment settings in appsettings.json.");
            }
        }

        /// <summary>
        /// Routes to the correct orchestration method based on the ACH step.
        /// </summary>
        private static async Task ExecuteOrchestrationAsync(object factory, ACHStep step, OrchestrationPromptInput input)
        {
            switch (step)
            {
                case ACHStep.HypothesisGeneration:
                    await ExecuteHypothesisGenerationAsync(factory, input);
                    break;

                case ACHStep.EvidenceExtraction:
                    await ExecuteEvidenceExtractionAsync(factory, input);
                    break;

                default:
                    throw new NotSupportedException($"ACH Step {step} is not yet implemented.");
            }
        }

        /// <summary>
        /// Runs the hypothesis generation step and displays the results.
        /// </summary>
        private static async Task ExecuteHypothesisGenerationAsync(object factory, OrchestrationPromptInput input)
        {
            var hypothesisFactory = factory as IOrchestrationFactory<List<Hypothesis>>
                ?? throw new InvalidOperationException("Factory is not of the expected type for Hypothesis Generation.");

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
            Console.WriteLine($"\n{new string('=', Constants.SeparatorLength)}");
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
            Console.WriteLine($"\n{new string('=', Constants.SeparatorLength)}");
            Console.WriteLine("\nExtracted Evidence:");

            foreach (var item in evidence)
            {
                Console.WriteLine($"Type: {item.Type}\tDescription: {item.Description}\n");
            }
        }

        /// <summary>
        /// Extracts the ACH step number and experiment index from command-line arguments.
        /// </summary>
        private static (int? achStepNumber, int? experimentIndex) ParseCommandLineArguments(string[] args)
        {
            int? achStepNumber = null;
            int? experimentIndex = null;

            if (args.Length >= 1 && int.TryParse(args[0], out int stepNum))
            {
                achStepNumber = stepNum;
            }

            if (args.Length >= 2 && int.TryParse(args[1], out int expIndex))
            {
                experimentIndex = expIndex;
            }

            return (achStepNumber, experimentIndex);
        }

        /// <summary>
        /// Registers configuration settings in the dependency injection container.
        /// </summary>
        private static void RegisterConfiguration(IServiceCollection services, IConfiguration configuration, int? achStepNumber, int? experimentIndex)
        {
            services.Configure<AIServiceSettings>(configuration.GetSection("AIServiceSettings"));

            var achStepName = DetermineAchStepName(achStepNumber);
            services.Configure<ACHStepSettings>(configuration.GetSection(achStepName));

            services.AddSingleton<ExperimentConfiguration>(sp =>
                BuildExperimentConfiguration(sp, achStepNumber, experimentIndex));

            services.AddSingleton<IEnumerable<AgentConfiguration>>(sp =>
            {
                var experimentConfig = sp.GetRequiredService<ExperimentConfiguration>();
                return experimentConfig.AgentConfigurations;
            });

            services.AddSingleton<IOptions<OrchestrationSettings>>(sp =>
            {
                var experimentConfig = sp.GetRequiredService<ExperimentConfiguration>();
                return Options.Create(experimentConfig.OrchestrationSettings);
            });

            services.AddSingleton<AIServiceSettings>(sp =>
            {
                var experimentConfig = sp.GetRequiredService<ExperimentConfiguration>();
                return experimentConfig.GlobalAIServiceSettings;
            });
        }

        /// <summary>
        /// Builds the ACH step configuration key name from the step number or environment.
        /// </summary>
        private static string DetermineAchStepName(int? achStepNumber)
        {
            if (achStepNumber.HasValue)
            {
                return $"{Constants.AchStepPrefix}{achStepNumber.Value}";
            }

            return Environment.GetEnvironmentVariable(Constants.DefaultAchStepEnvironmentVariable)
                ?? Constants.DefaultAchStep;
        }

        /// <summary>
        /// Loads and creates the experiment configuration for the specified ACH step.
        /// </summary>
        private static ExperimentConfiguration BuildExperimentConfiguration(IServiceProvider sp, int? achStepNumber, int? experimentIndex)
        {
            var config = sp.GetRequiredService<IConfiguration>();

            var globalAISettings = config.GetSection("AIServiceSettings").Get<AIServiceSettings>()
                ?? throw new InvalidOperationException("AIServiceSettings section not found in configuration");

            var achStepName = DetermineAchStepName(achStepNumber);

            var achStepSettings = config.GetSection(achStepName).Get<ACHStepSettings>()
                ?? throw new InvalidOperationException($"{achStepName} section not found in configuration");

            var experiment = SelectExperiment(achStepSettings, achStepName, experimentIndex);

            Console.WriteLine($"Selected experiment: {experiment.Name} - {experiment.Description}");

            experiment.GlobalAIServiceSettings = globalAISettings;

            return experiment;
        }

        /// <summary>
        /// Chooses an experiment by index or name from the ACH step settings.
        /// </summary>
        private static ExperimentConfiguration SelectExperiment(ACHStepSettings achStepSettings, string achStepName, int? experimentIndex)
        {
            if (experimentIndex.HasValue)
            {
                return SelectExperimentByIndex(achStepSettings, achStepName, experimentIndex.Value);
            }

            return SelectExperimentByName(achStepSettings, achStepName);
        }

        /// <summary>
        /// Gets an experiment from the settings using its array position.
        /// </summary>
        private static ExperimentConfiguration SelectExperimentByIndex(ACHStepSettings achStepSettings, string achStepName, int index)
        {
            if (achStepSettings.Experiments == null || index < 0 || index >= achStepSettings.Experiments.Count())
            {
                throw new InvalidOperationException(
                    $"Invalid experiment index {index}. {achStepName} has {achStepSettings.Experiments?.Count() ?? 0} experiments.");
            }

            return achStepSettings.Experiments[index];
        }

        /// <summary>
        /// Finds an experiment in the settings by matching its name.
        /// </summary>
        private static ExperimentConfiguration SelectExperimentByName(ACHStepSettings achStepSettings, string achStepName)
        {
            var experimentName = Environment.GetEnvironmentVariable(Constants.ExperimentNameEnvironmentVariable)
                ?? Constants.DefaultExperimentName;

            return achStepSettings.Experiments?.FirstOrDefault(e => e.Name == experimentName)
                ?? achStepSettings.Experiments?.FirstOrDefault()
                ?? throw new InvalidOperationException($"No experiment found with name '{experimentName}' in {achStepName}");
        }

        /// <summary>
        /// Registers Semantic Kernel services and AI adapters in the container.
        /// </summary>
        private static void RegisterKernelServices(IServiceCollection services)
        {
            // KernelBuilderService builds a default kernel for orchestration (e.g., structured output)
            services.AddSingleton<IKernelBuilderService, KernelBuilderService>();

            // AgentService builds individual kernels per agent based on ServiceId in appsettings
            services.AddSingleton<IAgentService, AgentService>();

            services.AddSingleton<ConsoleFormatter>();
        }

        /// <summary>
        /// Registers orchestration factories for each ACH step in the container.
        /// </summary>
        private static void RegisterOrchestrationServices(IServiceCollection services)
        {
            services.AddTransient<IOrchestrationFactory<List<Hypothesis>>, HypothesisGenerationOrchestrationFactory>();
            services.AddTransient<IOrchestrationFactory<List<Evidence>>, EvidenceExtractionOrchestrationFactory>();
            services.AddSingleton<IOrchestrationFactorySelector, OrchestrationFactorySelector>();
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
