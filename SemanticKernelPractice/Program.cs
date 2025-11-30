using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
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

            var kernelService = host.Services.GetRequiredService<IKernelBuilderService>();
            Console.WriteLine($"Using AI Provider: {kernelService.CurrentProvider}\n");

            Console.WriteLine("Task: Create a slogan for a premium smartwatch targeting fitness enthusiasts.\n");
            Console.WriteLine(new string('=', 70));

            try
            {
                var factory = host.Services.GetRequiredService<IOrchestrationFactory<string>>();

                var result = await factory.ExecuteCoreAsync("Create a slogan for a premium smartwatch targeting fitness enthusiasts.");

                Console.WriteLine("\n" + new string('=', 70));
                Console.WriteLine($"FINAL APPROVED SLOGAN: {result}");
                Console.WriteLine(new string('=', 70) + "\n");

                if (factory is SloganOrchestrationFactory sloganFactory)
                {
                    Console.WriteLine("Chat History:");
                    foreach (var entry in sloganFactory.GetHistory())
                    {
                        Console.WriteLine($"{entry.AuthorName}: {entry.Content}");
                    }
                }
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
                    config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
                    config.AddJsonFile($"appsettings.{context.HostingEnvironment.EnvironmentName}.json", optional: true, reloadOnChange: true);
                    config.AddEnvironmentVariables();
                })
                .ConfigureServices((context, services) =>
                {
                    // Register configuration settings
                    services.Configure<AIServiceSettings>(context.Configuration.GetSection("AIService"));
                    services.Configure<OrchestrationSettings>(context.Configuration.GetSection("OrchestrationSettings"));

                    // Register AgentConfiguration array as a direct service (IOptions doesn't support arrays)
                    services.AddSingleton<IEnumerable<AgentConfiguration>>(sp =>
                    {
                        var config = sp.GetRequiredService<IConfiguration>();
                        return config.GetSection("AgentConfigurations").Get<AgentConfiguration[]>() ?? Array.Empty<AgentConfiguration>();
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

                    // Register orchestration factories
                    services.AddTransient<IOrchestrationFactory<string>, SloganOrchestrationFactory>();

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
