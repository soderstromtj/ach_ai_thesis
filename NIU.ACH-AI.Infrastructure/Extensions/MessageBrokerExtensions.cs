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
                x.AddConsumer<Messaging.Consumers.SingleEvidenceEvaluationConsumer>();
                x.AddConsumer<Messaging.Consumers.EvidenceEvaluationResultConsumer>();

                // Register the Request Clients
                x.AddRequestClient<Application.Messaging.Commands.IBrainstormingRequested>();
                x.AddRequestClient<Application.Messaging.Commands.IHypothesisRefinementRequested>();
                x.AddRequestClient<Application.Messaging.Commands.IEvidenceExtractionRequested>();
                x.AddRequestClient<Application.Messaging.Commands.IEvidenceEvaluationRequested>();

                // Register Saga
                x.AddSagaStateMachine<StateMachines.ACHWorkflowStateMachine, Persistence.Models.ExperimentState, StateMachines.ACHWorkflowStateMachineDefinition>()
                    .EntityFrameworkRepository(r =>
                    {
                        r.ExistingDbContext<Persistence.Models.ACHSagaDbContext>();
                        r.UseSqlServer();
                    });

                x.UsingRabbitMq((context, cfg) =>
                {
                    // Read connection string safely without calling GetConnectionString which
                    // invokes GetSection(...) and can throw with a plain Mock<IConfiguration>.
                    var connectionString = configuration?["ConnectionStrings:messaging"];

                    // Global Retry Policy for Concurrency
                    cfg.UseMessageRetry(r => 
                    {
                        // Retry on EF Core Concurrency Exception and generic update errors (deadlocks)
                        r.Handle<Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException>();
                        r.Handle<Microsoft.EntityFrameworkCore.DbUpdateException>();
                        
                        // Robust Retry: 20 times, up to 500ms jitter.
                        // Ideally exponential for high contention:
                        r.Exponential(20, TimeSpan.FromMilliseconds(50), TimeSpan.FromMilliseconds(500), TimeSpan.FromMilliseconds(100));
                    });

                    // Optional: fallback to RabbitMQ:Host setting if provided (test sets this)
                    if (string.IsNullOrEmpty(connectionString))
                    {
                        connectionString = configuration?["RabbitMQ:Host"];
                    }

                    if (!string.IsNullOrEmpty(connectionString))
                    {
                        // If connectionString is just a host (e.g. "localhost") this overload works.
                        cfg.Host(connectionString);
                    }
                    else
                    {
                        // Fallback to localhost if no connection string or host is provided
                        cfg.Host("localhost", "/", h =>
                        {
                            h.Username("guest");
                            h.Password("guest");
                        });
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
