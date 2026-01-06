using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NIU.ACH_AI.Application.Interfaces;
using NIU.ACH_AI.Infrastructure.Persistence.Models;
using NIU.ACH_AI.Infrastructure.Persistence.Repositories;
using NIU.ACH_AI.Infrastructure.Persistence.Services;

namespace NIU.ACH_AI.Infrastructure.Persistence
{
    /// <summary>
    /// Extension methods for setting up persistence services in the dependency injection container.
    /// </summary>
    public static class DependencyInjection
    {
        /// <summary>
        /// Registers persistence-related services, repositories, and the database context.
        /// </summary>
        /// <param name="services">The service collection to add services to.</param>
        /// <param name="configuration">The configuration containing database connection strings.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddPersistence(this IServiceCollection services, IConfiguration configuration)
        {
            // Register DbContext
            services.AddDbContext<AchAIDbContext>(options =>
                options.UseSqlServer(configuration.GetConnectionString("AchAiDBConnection")));

            services.AddDbContext<ACHSagaDbContext>(options =>
                options.UseSqlServer(configuration.GetConnectionString("AchAiDBConnection")));

            // Register Repositories
            services.AddScoped<IHypothesisRepository, HypothesisRepository>();
            services.AddScoped<IEvidenceRepository, EvidenceRepository>();
            services.AddScoped<IEvidenceHypothesisEvaluationRepository, EvidenceHypothesisEvaluationRepository>();
            services.AddScoped<IAgentConfigurationPersistence, AgentConfigurationPersistence>();
            services.AddScoped<IWorkflowResultPersistence, WorkflowResultPersistence>();
            services.AddScoped<IWorkflowPersistence, WorkflowPersistence>();
            
            // Register Singletons
            services.AddSingleton<AgentResponsePersistence>();
            services.AddSingleton<IAgentResponsePersistence, AgentResponsePersistence>();

            return services;
        }
    }
}
