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
using NIU.ACH_AI.Infrastructure.Extensions;

namespace NIU.ACH_AI.FrontendConsole.Extensions
{
    /// <summary>
    /// Adds frontend-specific services and settings to the application.
    /// </summary>
    public static class DependencyInjection
    {
        /// <summary>
        /// Adds frontend services, settings, and logging to the service collection.
        /// </summary>
        /// <param name="services">The service collection to add services to.</param>
        /// <param name="configuration">The configuration containing app settings.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddFrontendServices(this IServiceCollection services, IConfiguration configuration)
        {
            // Register Configuration
            services.Configure<ExperimentsSettings>(configuration);
            services.Configure<AIServiceSettings>(configuration.GetSection("AIServiceSettings"));

            // Register Message Broker
            services.AddMessageBroker(configuration);

            // Register HttpClient
            services.AddHttpClient();

            // Register Services
            services.AddSingleton<IKernelBuilderService, KernelBuilderService>();
            services.AddScoped<IOrchestrationExecutor, OrchestrationExecutor>();
            services.AddScoped<IOrchestrationFactoryProvider, OrchestrationFactoryProvider>();
            services.AddSingleton<ITokenUsageExtractor, TokenUsageExtractor>();
            services.AddScoped<IExperimentInitializationService, ExperimentInitializationService>();
            services.AddScoped<IExperimentMonitoringService, ExperimentMonitoringService>();
            services.AddScoped<IOrchestrationPromptFormatter, OrchestrationPromptFormatter>();
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
