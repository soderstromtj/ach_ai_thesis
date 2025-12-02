using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.Agents.AzureAI;
using Microsoft.SemanticKernel.Agents.Orchestration;
using Microsoft.SemanticKernel.Agents.Orchestration.GroupChat;
using Microsoft.SemanticKernel.Agents.Orchestration.Transforms;
using Microsoft.SemanticKernel.Agents.Runtime.InProcess;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using SemanticKernelPractice.Configuration;
using SemanticKernelPractice.Factories;
using SemanticKernelPractice.Models;
using SemanticKernelPractice.Services;
using SemanticKernelPractice.Services.KernelBuilders;

namespace SemanticKernelPractice
{

#pragma warning disable SKEXP0110 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

    class Program
    {
        static async Task Main(string[] args)
        {
            var host = CreateHostBuilder(args).Build();

            Console.WriteLine("=== Application started. Press Ctrl+C to shut down. ===");

            var experimentConfig = host.Services.GetRequiredService<ExperimentConfiguration>();
            Console.WriteLine($"Experiment: {experimentConfig.Name}");
            Console.WriteLine($"AI Provider: {experimentConfig.Provider}\n");

            Console.WriteLine("Task: Extracting evidence for ACH step 2.\n");
            Console.WriteLine(new string('=', 70));

            try
            {
                var factory = host.Services.GetRequiredService<IOrchestrationFactory<List<Evidence>>>();

                // Get KIQ, context and task instructions from experiment configuration
                var kiq = experimentConfig.
                var context = experimentConfig.Context;
                var instructions = experimentConfig.TaskInstructions;

                if (string.IsNullOrWhiteSpace(context))
                {
                    throw new InvalidOperationException("Context is not configured for this experiment. Please add 'Context' to the experiment settings in appsettings.json.");
                }

                if (string.IsNullOrWhiteSpace(instructions))
                {
                    throw new InvalidOperationException("Task instructions are not configured for this experiment. Please add 'TaskInstructions' to the experiment settings in appsettings.json.");
                }

                var input = $"Context: {context}\nInstructions: {instructions}";

                Console.WriteLine("\nExecuting Evidence Extraction Orchestration...\n");

                var result = await factory.ExecuteCoreAsync(input);

                Console.WriteLine("\n" + new string('=', 70));
                Console.WriteLine("\nList of Evidence");
                foreach (Evidence evidence in result)
                {
                    Console.WriteLine($"Type: {evidence.Type}\tDescription: {evidence.Description}\n");
                }
                Console.WriteLine(new string('=', 70) + "\n");
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

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration((context, config) =>
                {
                    config.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
                    config.AddJsonFile($"appsettings.{context.HostingEnvironment.EnvironmentName}.json", optional: true, reloadOnChange: true);
                    config.AddEnvironmentVariables();
                })
                .ConfigureServices((context, services) =>
                {
                    // Register global AI service settings (from AIServiceSettings section)
                    services.Configure<AIServiceSettings>(context.Configuration.GetSection("AIServiceSettings"));

                    // Register ACH Step 2 settings
                    services.Configure<ACHStep2Settings>(context.Configuration.GetSection("ACHStep2"));

                    // Build and register ExperimentConfiguration based on selected experiment
                    services.AddSingleton<ExperimentConfiguration>(sp =>
                    {
                        var config = sp.GetRequiredService<IConfiguration>();

                        // Load global AI service settings
                        var globalAISettings = config.GetSection("AIServiceSettings").Get<AIServiceSettings>()
                            ?? throw new InvalidOperationException("AIServiceSettings section not found in configuration");

                        // Load ACH Step 2 settings
                        var achStep2Settings = config.GetSection("ACHStep2").Get<ACHStep2Settings>()
                            ?? throw new InvalidOperationException("ACHStep2 section not found in configuration");

                        // Get experiment name from environment variable or use default
                        var experimentName = Environment.GetEnvironmentVariable("ACH_EXPERIMENT_NAME") ?? "Baseline";

                        // Find the experiment by name
                        var experiment = achStep2Settings.Experiments?.FirstOrDefault(e => e.Name == experimentName)
                            ?? achStep2Settings.Experiments?.FirstOrDefault()
                            ?? throw new InvalidOperationException($"No experiment found with name '{experimentName}'");

                        Console.WriteLine($"Selected experiment: {experiment.Name} - {experiment.Description}");

                        // Inject global AI service settings into the experiment configuration
                        experiment.GlobalAIServiceSettings = globalAISettings;

                        return experiment;
                    });

                    // For backward compatibility, register AgentConfiguration array and OrchestrationSettings from ExperimentConfiguration
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

                    // Register kernel builder adapters
                    services.AddSingleton<IKernelBuilderAdapter, AzureOpenAIKernelAdapter>();
                    services.AddSingleton<IKernelBuilderAdapter, HuggingFaceKernelAdapter>();
                    services.AddSingleton<IKernelBuilderAdapter, OllamaKernelAdapter>();
                    services.AddSingleton<IKernelBuilderAdapter, OpenAIKernelAdapter>();

                    // Register kernel builder service (uses adapters)
                    services.AddSingleton<IKernelBuilderService, KernelBuilderService>();

                    // Register Kernel as a singleton using the KernelBuilderService
                    services.AddSingleton<Kernel>(sp =>
                    {
                        var kernelBuilder = sp.GetRequiredService<IKernelBuilderService>();
                        return kernelBuilder.BuildKernel();
                    });

                    // Register agent creation service (uses kernel builder service)
                    services.AddSingleton<IAgentService, AgentService>();

                    // Register workflow logging services
                    services.AddSingleton<ConsoleFormatter>();
                    services.AddScoped<WorkflowLogger>();

                    // Register orchestration factories
                    services.AddTransient<IOrchestrationFactory<List<Evidence>>, EvidenceExtractionOrchestrationFactory>();

                    // Configure logging
                    services.AddLogging(builder =>
                    {
                        builder.AddConsole();
                        builder.AddDebug();
                        builder.AddConfiguration(context.Configuration.GetSection("LoggingSettings"));
                    });
                });
    } 
}


    

#pragma warning restore SKEXP0110 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
