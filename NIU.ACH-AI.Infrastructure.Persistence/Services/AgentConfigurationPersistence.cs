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

        /// <summary>
        /// Initializes a new instance of the <see cref="AgentConfigurationPersistence"/> class.
        /// </summary>
        /// <param name="context">The database context.</param>
        public AgentConfigurationPersistence(DbModel.AchAIDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        /// <summary>
        /// Creates and persists agent configurations for a specific step execution.
        /// validating that the referenced providers and models exist.
        /// </summary>
        /// <param name="stepExecutionId">The unique identifier of the step execution.</param>
        /// <param name="agentConfigurations">The collection of agent configurations to persist.</param>
        /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
        /// <returns>A dictionary mapping agent names to their persisted configuration IDs.</returns>
        /// <exception cref="ArgumentException">Thrown when required arguments are invalid or missing.</exception>
        /// <exception cref="InvalidOperationException">Thrown when referenced providers/models are not found or database update fails.</exception>
        public async Task<IReadOnlyDictionary<string, Guid>> CreateAgentConfigurationsAsync(
            Guid stepExecutionId,
            IEnumerable<AgentConfiguration> agentConfigurations,
            CancellationToken cancellationToken = default)
        {
            if (stepExecutionId == Guid.Empty)
            {
                throw new ArgumentException("Step execution ID must be provided.", nameof(stepExecutionId));
            }

            if (agentConfigurations == null || !agentConfigurations.Any())
            {
                throw new ArgumentNullException(nameof(agentConfigurations), "Agent configurations must be provided.");
            }

            var configurations = agentConfigurations.ToList();

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
