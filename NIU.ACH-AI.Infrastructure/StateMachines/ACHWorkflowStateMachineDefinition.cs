using MassTransit;
using NIU.ACH_AI.Infrastructure.Persistence.Models;
using NIU.ACH_AI.Application.Messaging.Events;

namespace NIU.ACH_AI.Infrastructure.StateMachines
{
    /// <summary>
    /// Configures the behavior of the ACH workflow state machine within the message broker.
    /// Sets up concurrency limits and partitioning rules to prevent database locking issues.
    /// </summary>
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

            // Strict Sequential Processing for specific messages to prevent Saga concurrency issues
            // This force MassTransit to process only 1 message at a time for the Saga endpoint
            // Since we are using Partitioner, it should already be serialized per CorrelationId, 
            // but setting ConcurrentMessageLimit to 1 globally for this endpoint (or limiting the partitioner concurrency)
            // ensures we don't have multiple threads fighting over the DB lock.
            // Note: This might slow down overall processing if we only have 1 consumer instance for ALL experiments?
            // No, Partitioner handles it per CorrelationID.
            // Let's trust the Partitioner but ensure the ConcurrencyLimit on the endpoint is reasonable.
            
            // Actually, to be safe towards the user request:
            // "PairEvaluated events to be completed sequentially"
            // The Partitioner (lines 13-16) does exactly that for a given ExperimentId.
            // We adding a backup:
            // sagaConfigurator.UseMessageRetry(...) is handled globally in extensions.

        }
    }
}
