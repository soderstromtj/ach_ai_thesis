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

            Console.WriteLine("Task: Extracting evidence for ACH step 2.\n");
            Console.WriteLine(new string('=', 70));

            try
            {
                var factory = host.Services.GetRequiredService<IOrchestrationFactory<List<Evidence>>>();

                var context = "**Jousting with Cuba over Radio Marti**\\n\\n**Key Questions:** How would Fidel Castro react to Radio Marti, how might he block or delay it, would his strategy be proactive or reactive, and what risks and benefits would the broadcasts pose for the Reagan administration?\\n\\n**A Modest Skirmish in a Global Confrontation**  \\nWhen Ronald Reagan took office in 1981, the United States and Cuba had been in confrontation for two decades. Castro increasingly interfered with US AM radio; by the 1970s it was serious. In 1979 Cuba announced plans for high-power transmitters far above US limits. Confronting Soviet allies was central to Reagan’s strategy; breaking Havana’s monopoly on domestic information through Radio Marti would be a symbolic tool and would appeal to Cuban American opinion in Florida.\\n\\n**Castro’s Threat and US Stakeholders**  \\nAs plans took shape, Castro publicly threatened to disrupt AM broadcasting across the United States if Radio Marti went on the air. Broadcasters feared economic losses; the National Association of Broadcasters (NAB) led a lobbying effort. For Reagan, the issue was personal: in the 1930s he had been a sports announcer at WHO, a clear-channel station on 1040 kilohertz (kHz) in Des Moines. Cuba later signaled plans to broadcast on 1040 with far greater power, and on 30 August 1982 a Cuban transmission on that frequency disrupted WHO. The Cuban American exile community strongly backed Radio Marti as a challenge to Castro’s control of information. South Florida broadcasters supported the project, while midwestern farmers and truckers, dependent on clear-channel radio, were wary, and Congress reflected that divide.\\n\\n**From Proposal to Compromise**  \\nOn 22 September 1981, Reagan signed an executive order creating a Presidential Commission on Broadcasting to Cuba to design a plan “to promote open communication of information and ideas to Cuba.” The commission recommended a new service, and in 1982 the House passed a bill authorizing Radio Marti to broadcast on 1040 kHz and urging a solution to Cuban interference. Cuba then announced that transmitters would move to 1040 and 1160 kHz and, in August 1982, disrupted WHO and other US stations. The NAB mobilized, and the Senate declined to act on the House bill.\\n\\nIn the new Congress, Radio Marti was revived but remained controversial, and US–Cuban talks on interference in 1983 failed. A Senate compromise finally emerged: Radio Marti would operate under Voice of America standards and leave WHO’s frequency so as not to threaten domestic clear-channel service. With that change, the bill passed both houses, and President Reagan signed it into law on 4 October 1983, establishing Radio Marti as a US-sponsored broadcaster to Cuba.\\n\\n**Two Central Questions and a Radio War?**  \\nAuthorization did not end the problem. The administration still had to define the station’s content and decide how aggressive programming should be. Analysts knew that Cuba had publicly threatened retaliation, possessed high-power transmitters, and had demonstrated the ability to disrupt US commercial radio. The remaining questions were how Castro would respond and how the United States might influence that choice.\"\r\n";
                var instructions = "Tasking: Extract Evidence From the Context for ACH\\n\\n1. Objective\\n\\nYou will review the provided document (the \\\"context\\\") and produce one consolidated list of Evidence items in JSON format. This list will become the left-hand column of an Analysis of Competing Hypotheses (ACH) matrix.\\n\\nYour job is to identify facts and key assumptions only. You are not to discuss or compare hypotheses in this task.\\n\\nYour final JSON should have a single top-level key \\\"Evidence\\\" whose value is a list of objects, each with:\\n- EvidenceID: a unique integer starting at 1 and increasing by 1,\\n- Type: \\\"Fact\\\" or \\\"Assumption\\\",\\n- Description: a concise, neutral statement expressing a single idea.\\n\\n2. What Counts as Evidence\\n\\nFor this task, an Evidence item is:\\n- A Fact directly supported by the context or by well established background information, or\\n- A Key Assumption that you must treat as true to interpret the situation or connect facts.\\n\\nEach item must be:\\n- Atomic - one main idea only.\\n- Traceable - you can point to where it comes from in the context or clearly state it as an assumption.\\n- Neutral - avoids wording that embeds a conclusion or favors any hypothesis.\\n- Balanced - includes both information that could support and that could challenge plausible hypotheses.\\n\\n3. Roles\\n\\nEveryone contributes to identifying and refining Evidence items, but with different emphases:\\n- Facilitator: guides the process, manages time, keeps focus on evidence (not hypotheses), and decides when the list of Evidence is sufficient and there is no more to extract.\\n- Domain Subject-Matter Expert: clarifies context and what is operationally important, and highlights domain-relevant nuances.\\n- Textual Forensic Analyst: tracks exactly what the text says, where, and with what level of certainty, and helps phrase atomic, traceable Evidence items.\\n- Contrarian / Red-Team Analyst: challenges whether items are true evidence, checks for bias, over-interpretation, and missing disconfirming items.\\n- Assumption and Bias Auditor: surfaces hidden assumptions, labels them explicitly as Assumptions, and calls out cognitive biases in how evidence is selected or worded.\\n\\n4. Facilitator Responsibility\\n\\nAs Facilitator, you are responsible for:\\n- Keeping the group strictly focused on evidence extraction, not on arguing or evaluating hypotheses.\\n- Periodically asking whether there is any additional fact or assumption in the context that the group has not yet captured as an Evidence item.\\n- Ensuring each Evidence item is atomic, traceable, neutral, and clearly labeled as \\\"Fact\\\" or \\\"Assumption\\\".\\n- Deciding, after consulting the group, when:\\n  - The evidence list is sufficiently comprehensive,\\n  - No substantial new items are being identified, and\\n  - It is appropriate to stop extraction and finalize the Evidence JSON.\\n\\nOnce you, as Facilitator, declare the list complete, the Evidence JSON is locked and will be used as the fixed left-hand column for subsequent ACH steps.\r\n";
                var input = $"Context: {context}\nInstructions: {instructions}";

                Console.WriteLine("\nExecuting Evidence Extraction Orchestration...\n");
                Console.WriteLine($"Input: {input}", 0, 250);

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
