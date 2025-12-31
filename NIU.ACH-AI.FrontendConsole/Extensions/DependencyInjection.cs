using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NIU.ACH_AI.Application.Configuration;
using NIU.ACH_AI.Application.Interfaces;
using NIU.ACH_AI.Application.Services;
using NIU.ACH_AI.FrontendConsole.Presentation;
using NIU.ACH_AI.Infrastructure.AI.Factories;
using NIU.ACH_AI.Infrastructure.AI.Services;
using NIU.ACH_AI.Infrastructure.Configuration;

namespace NIU.ACH_AI.FrontendConsole.Extensions
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddFrontendServices(this IServiceCollection services, IConfiguration configuration)
        {
            // Register Configuration
            services.Configure<ExperimentsSettings>(configuration);
            services.Configure<AIServiceSettings>(configuration.GetSection("AIServiceSettings"));

            // Register Services
            services.AddSingleton<IKernelBuilderService, KernelBuilderService>();
            services.AddSingleton<IOrchestrationExecutor, OrchestrationExecutor>();
            services.AddSingleton<IOrchestrationFactoryProvider, OrchestrationFactoryProvider>();
            services.AddSingleton<ITokenUsageExtractor, TokenUsageExtractor>();
            services.AddScoped<IACHWorkflowCoordinator, ACHWorkflowCoordinator>();
            services.AddTransient<ConsoleResultPresenter>();

            // Register Logging
            services.AddLogging(builder =>
            {
                builder.AddConsole();
                builder.AddDebug();
                builder.AddConfiguration(configuration.GetSection("Logging"));
            });

            return services;
        }
    }
}
