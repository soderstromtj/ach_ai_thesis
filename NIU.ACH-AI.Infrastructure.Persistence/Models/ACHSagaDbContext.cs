using System.Collections.Generic;
using MassTransit.EntityFrameworkCoreIntegration;
using Microsoft.EntityFrameworkCore;
using NIU.ACH_AI.Infrastructure.Persistence.Configurations;

namespace NIU.ACH_AI.Infrastructure.Persistence.Models
{
    public class ACHSagaDbContext : SagaDbContext
    {
        public ACHSagaDbContext(DbContextOptions<ACHSagaDbContext> options) : base(options)
        {
        }

        protected override IEnumerable<ISagaClassMap> Configurations
        {
            get { yield return new ExperimentStateMap(); }
        }
    }
}
