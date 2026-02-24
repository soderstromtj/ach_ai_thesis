using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NIU.ACH_AI.Infrastructure.StateMachines;

namespace NIU.ACH_AI.Infrastructure.Persistence.Configurations
{
    /// <summary>
    /// Configures the database mapping for the saga experiment state.
    /// Specifies how properties are stored and constraint rules like string length.
    /// </summary>
    public class ExperimentStateMap : SagaClassMap<ExperimentState>
    {
        protected override void Configure(EntityTypeBuilder<ExperimentState> entity, ModelBuilder model)
        {
            entity.Property(x => x.CurrentState).HasMaxLength(64);
            entity.Property(x => x.CurrentStepIndex);
            
            // Serialize complex objects as text/json
            entity.Property(x => x.SerializedConfiguration).IsRequired();
            entity.Property(x => x.SerializedInput).IsRequired();
            entity.Property(x => x.SerializedResult).IsRequired();

            entity.Property(x => x.Created);
            entity.Property(x => x.Updated);
            
            entity.Property(x => x.HypothesisStepExecutionId);
            entity.Property(x => x.RefinedHypothesisStepExecutionId);
            entity.Property(x => x.EvidenceStepExecutionId);
        }
    }
}
