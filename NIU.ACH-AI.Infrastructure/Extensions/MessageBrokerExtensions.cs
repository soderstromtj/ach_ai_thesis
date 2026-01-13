using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace NIU.ACH_AI.Infrastructure.Extensions
{
    public static class MessageBrokerExtensions
    {
        public static IServiceCollection AddMessageBroker(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddMassTransit(x =>
            {
                // Register the consumer
                x.AddConsumer<Messaging.Consumers.HypothesisBrainstormingConsumer>();
                x.AddConsumer<Messaging.Consumers.HypothesisRefinementConsumer>();
                x.AddConsumer<Messaging.Consumers.EvidenceExtractionConsumer>();
                x.AddConsumer<Messaging.Consumers.EvidenceEvaluationConsumer>();
                x.AddConsumer<Messaging.Consumers.AgentResponsePersistenceConsumer>();

                // Register the Request Clients
                x.AddRequestClient<Application.Messaging.Commands.IBrainstormingRequested>();
                x.AddRequestClient<Application.Messaging.Commands.IHypothesisRefinementRequested>();
                x.AddRequestClient<Application.Messaging.Commands.IEvidenceExtractionRequested>();
                x.AddRequestClient<Application.Messaging.Commands.IEvidenceEvaluationRequested>();

                // Register Saga
                x.AddSagaStateMachine<StateMachines.ACHWorkflowStateMachine, Persistence.Models.ExperimentState>()
                    .EntityFrameworkRepository(r =>
                    {
                        r.ExistingDbContext<Persistence.Models.ACHSagaDbContext>();
                        r.UseSqlServer();
                    });

                x.UsingRabbitMq((context, cfg) =>
                {
                    var connectionString = configuration.GetConnectionString("messaging");
                    if (!string.IsNullOrEmpty(connectionString))
                    {
                        cfg.Host(new Uri(connectionString));
                    }

                    // Explicitly configure the persistence queue
                    cfg.ReceiveEndpoint("q.ach.persistence", e =>
                    {
                        e.ConfigureConsumer<Messaging.Consumers.AgentResponsePersistenceConsumer>(context);
                    });

                    // Configure endpoints automatically for the rest
                    cfg.ConfigureEndpoints(context);
                });
            });

            return services;
        }
    }
}
