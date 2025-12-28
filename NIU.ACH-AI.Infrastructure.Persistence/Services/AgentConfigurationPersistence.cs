using Microsoft.EntityFrameworkCore;
using NIU.ACH_AI.Application.Configuration;
using NIU.ACH_AI.Application.Interfaces;
using DbModel = NIU.ACH_AI.Infrastructure.Persistence.Models;

namespace NIU.ACH_AI.Infrastructure.Persistence.Services
{
    /// <summary>
    /// Persists agent configuration metadata for a step execution.
    /// </summary>
    public class AgentConfigurationPersistence : IAgentConfigurationPersistence
    {
        private readonly DbModel.AchAIDbContext _context;

        public AgentConfigurationPersistence(DbModel.AchAIDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public async Task<IReadOnlyDictionary<string, Guid>> CreateAgentConfigurationsAsync(
            Guid stepExecutionId,
            IEnumerable<AgentConfiguration> agentConfigurations,
            CancellationToken cancellationToken = default)
        {
            if (stepExecutionId == Guid.Empty)
            {
                throw new ArgumentException("Step execution ID must be provided.", nameof(stepExecutionId));
            }

            ArgumentNullException.ThrowIfNull(agentConfigurations, nameof(agentConfigurations));

            var configurations = agentConfigurations.ToList();
            if (configurations.Count == 0)
            {
                return new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);
            }

            var entities = new List<DbModel.AgentConfiguration>(configurations.Count);
            var agentIdMap = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);

            foreach (var config in configurations)
            {
                if (string.IsNullOrWhiteSpace(config.Name))
                {
                    throw new ArgumentException("Agent configuration name must be provided.", nameof(agentConfigurations));
                }

                if (string.IsNullOrWhiteSpace(config.ServiceId))
                {
                    throw new ArgumentException(
                        $"Agent configuration '{config.Name}' must specify a ServiceId.", nameof(agentConfigurations));
                }

                if (string.IsNullOrWhiteSpace(config.ModelId))
                {
                    throw new ArgumentException(
                        $"Agent configuration '{config.Name}' must specify a ModelId.", nameof(agentConfigurations));
                }

                if (agentIdMap.ContainsKey(config.Name))
                {
                    throw new ArgumentException(
                        $"Duplicate agent name '{config.Name}' detected in configuration.", nameof(agentConfigurations));
                }

                var provider = await _context.Providers
                    .AsNoTracking()
                    .FirstOrDefaultAsync(
                        p => p.ProviderName.ToLower() == config.ServiceId.ToLower(),
                        cancellationToken);

                if (provider == null)
                {
                    throw new InvalidOperationException(
                        $"Provider '{config.ServiceId}' not found. Ensure providers are seeded.");
                }

                var model = await _context.Models
                    .AsNoTracking()
                    .FirstOrDefaultAsync(
                        m => m.ProviderId == provider.ProviderId &&
                             m.ModelName.ToLower() == config.ModelId.ToLower(),
                        cancellationToken);

                if (model == null)
                {
                    throw new InvalidOperationException(
                        $"Model '{config.ModelId}' not found for provider '{provider.ProviderName}'. Ensure models are seeded.");
                }

                var agentConfigId = Guid.NewGuid();
                var entity = new DbModel.AgentConfiguration
                {
                    AgentConfigurationId = agentConfigId,
                    StepExecutionId = stepExecutionId,
                    AgentName = config.Name,
                    Description = config.Description ?? string.Empty,
                    Instructions = config.Instructions ?? string.Empty,
                    ProviderId = provider.ProviderId,
                    ModelId = model.ModelId,
                    CreatedAt = DateTime.UtcNow
                };

                entities.Add(entity);
                agentIdMap[config.Name] = agentConfigId;
            }

            try
            {
                _context.AgentConfigurations.AddRange(entities);
                await _context.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException ex)
            {
                throw new InvalidOperationException("Failed to persist agent configurations.", ex);
            }

            return agentIdMap;
        }
    }
}
