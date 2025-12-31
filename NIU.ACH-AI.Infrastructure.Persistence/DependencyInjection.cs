using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NIU.ACH_AI.Application.Interfaces;
using NIU.ACH_AI.Infrastructure.Persistence.Models;
using NIU.ACH_AI.Infrastructure.Persistence.Repositories;
using NIU.ACH_AI.Infrastructure.Persistence.Services;

namespace NIU.ACH_AI.Infrastructure.Persistence
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddPersistence(this IServiceCollection services, IConfiguration configuration)
        {
            // Register DbContext
            services.AddDbContext<AchAIDbContext>(options =>
                options.UseSqlServer(configuration.GetConnectionString("AchAiDBConnection")));

            // Register Repositories
            services.AddScoped<IHypothesisRepository, HypothesisRepository>();
            services.AddScoped<IEvidenceRepository, EvidenceRepository>();
            services.AddScoped<IEvidenceHypothesisEvaluationRepository, EvidenceHypothesisEvaluationRepository>();
            services.AddScoped<IAgentConfigurationPersistence, AgentConfigurationPersistence>();
            services.AddScoped<IWorkflowResultPersistence, WorkflowResultPersistence>();
            services.AddScoped<IWorkflowPersistence, WorkflowPersistence>();
            
            // Register Singletons
            services.AddSingleton<IAgentResponsePersistence, AgentResponsePersistence>();

            return services;
        }
    }
}
