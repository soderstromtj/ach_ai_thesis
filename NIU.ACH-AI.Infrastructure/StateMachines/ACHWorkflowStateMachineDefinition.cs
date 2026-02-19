using MassTransit;
using NIU.ACH_AI.Infrastructure.Persistence.Models;
using NIU.ACH_AI.Application.Messaging.Events;

namespace NIU.ACH_AI.Infrastructure.StateMachines
{
    public class ACHWorkflowStateMachineDefinition : SagaDefinition<ExperimentState>
    {
        protected override void ConfigureSaga(IReceiveEndpointConfigurator endpointConfigurator, ISagaConfigurator<ExperimentState> sagaConfigurator)
        {
            // Process messages for the same ExperimentId sequentially
            // This prevents "RowVersion" concurrency errors in the database
            var partitioner = endpointConfigurator.CreatePartitioner(8);

            sagaConfigurator.Message<IPairEvaluated>(x => x.UsePartitioner(partitioner, m => m.Message.ExperimentId));
            sagaConfigurator.Message<IEvaluationBatchStarted>(x => x.UsePartitioner(partitioner, m => m.Message.ExperimentId));

            // Use the Outbox to ensure events are only published if the database transaction commits
            endpointConfigurator.UseInMemoryOutbox();
        }
    }
}
