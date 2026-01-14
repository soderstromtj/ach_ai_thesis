using System;
using System.Text.Json;
using System.Collections.Generic;
using MassTransit;
using Microsoft.Extensions.Logging;
using NIU.ACH_AI.Application.Configuration;
using NIU.ACH_AI.Application.DTOs;
using NIU.ACH_AI.Application.Messaging.Commands;
using NIU.ACH_AI.Application.Messaging.Events;
using NIU.ACH_AI.Infrastructure.Persistence.Models;

namespace NIU.ACH_AI.Infrastructure.StateMachines
{
    public class ACHWorkflowStateMachine : MassTransitStateMachine<ExperimentState>
    {
        private readonly ILogger<ACHWorkflowStateMachine> _logger;

        // States
        public State Brainstorming { get; private set; }
        public State Refining { get; private set; }
        public State Extracting { get; private set; }
        public State Evaluating { get; private set; }
        public State Completed { get; private set; }
        public State Failed { get; private set; }

        // Events
        public Event<IExperimentStarted> ExperimentStarted { get; private set; }
        public Event<IBrainstormingResult> BrainstormingCompleted { get; private set; }
        public Event<IHypothesisRefinementResult> RefinementCompleted { get; private set; }
        public Event<IEvidenceExtractionResult> ExtractionCompleted { get; private set; }
        public Event<IEvidenceEvaluationResult> EvaluationCompleted { get; private set; }

        public ACHWorkflowStateMachine(ILogger<ACHWorkflowStateMachine> logger)
        {
            _logger = logger;

            InstanceState(x => x.CurrentState);

            // Correlate Events
            Event(() => ExperimentStarted, x => x.CorrelateById(m => m.Message.ExperimentId));
            Event(() => BrainstormingCompleted, x => x.CorrelateById(m => m.Message.ExperimentId));
            Event(() => RefinementCompleted, x => x.CorrelateById(m => m.Message.ExperimentId));
            Event(() => ExtractionCompleted, x => x.CorrelateById(m => m.Message.ExperimentId));
            Event(() => EvaluationCompleted, x => x.CorrelateById(m => m.Message.ExperimentId));

            // Initial State: Start Experiment
            Initially(
                DispatchStep(
                    When(ExperimentStarted)
                    .Then(ctx =>
                    {
                        _logger.LogInformation("Saga Started: Experiment {ExperimentId}", ctx.Message.ExperimentId);
                        ctx.Saga.CorrelationId = ctx.Message.ExperimentId;
                        ctx.Saga.Created = DateTime.UtcNow;
                        ctx.Saga.Updated = DateTime.UtcNow;
                        ctx.Saga.CurrentStepIndex = 0;
                        
                        ctx.Saga.SerializedConfiguration = JsonSerializer.Serialize(ctx.Message.Configuration);
                        
                        var input = new OrchestrationPromptInput
                        {
                            KeyQuestion = ctx.Message.Configuration.KeyQuestion,
                            Context = ctx.Message.Configuration.Context
                        };
                        ctx.Saga.SerializedInput = JsonSerializer.Serialize(input);

                        var result = new ACHWorkflowResult
                        {
                            ExperimentId = ctx.Message.ExperimentId.ToString(),
                            ExperimentName = ctx.Message.Configuration.Name
                        };
                        ctx.Saga.SerializedResult = JsonSerializer.Serialize(result);
                    })
                )
            );
            
            During(Brainstorming,
                DispatchStep(
                    When(BrainstormingCompleted)
                    .Then(ctx =>
                    {
                       if (!ctx.Message.Success) throw new Exception(ctx.Message.ErrorMessage);

                       var result = Deserialize<ACHWorkflowResult>(ctx.Saga.SerializedResult);
                       result.Hypotheses = ctx.Message.Hypotheses;
                       ctx.Saga.SerializedResult = JsonSerializer.Serialize(result);

                       var input = Deserialize<OrchestrationPromptInput>(ctx.Saga.SerializedInput);
                       input.HypothesisResult = new HypothesisResult { Hypotheses = result.Hypotheses };
                       ctx.Saga.SerializedInput = JsonSerializer.Serialize(input);
                       
                       ctx.Saga.HypothesisStepExecutionId = ctx.Message.StepExecutionId;
                       ctx.Saga.CurrentStepIndex++;
                    })
                )
            );

            During(Refining,
                DispatchStep(
                    When(RefinementCompleted)
                    .Then(ctx =>
                    {
                       if (!ctx.Message.Success) throw new Exception(ctx.Message.ErrorMessage);

                       var result = Deserialize<ACHWorkflowResult>(ctx.Saga.SerializedResult);
                       result.RefinedHypotheses = ctx.Message.RefinedHypotheses;
                       ctx.Saga.SerializedResult = JsonSerializer.Serialize(result);

                       var input = Deserialize<OrchestrationPromptInput>(ctx.Saga.SerializedInput);
                       input.HypothesisResult = new HypothesisResult { Hypotheses = result.RefinedHypotheses };
                       ctx.Saga.SerializedInput = JsonSerializer.Serialize(input);
                       
                       ctx.Saga.RefinedHypothesisStepExecutionId = ctx.Message.StepExecutionId;
                       ctx.Saga.CurrentStepIndex++;
                    })
                )
            );

            During(Extracting,
                DispatchStep(
                    When(ExtractionCompleted)
                    .Then(ctx =>
                    {
                       if (!ctx.Message.Success) throw new Exception(ctx.Message.ErrorMessage);

                       var result = Deserialize<ACHWorkflowResult>(ctx.Saga.SerializedResult);
                       result.Evidence = ctx.Message.Evidence;
                       ctx.Saga.SerializedResult = JsonSerializer.Serialize(result);

                       var input = Deserialize<OrchestrationPromptInput>(ctx.Saga.SerializedInput);
                       input.EvidenceResult = new EvidenceResult { Evidence = result.Evidence };
                       ctx.Saga.SerializedInput = JsonSerializer.Serialize(input);
                       
                       ctx.Saga.EvidenceStepExecutionId = ctx.Message.StepExecutionId;
                       ctx.Saga.CurrentStepIndex++;
                    })
                )
            );

            During(Evaluating,
                DispatchStep(
                    When(EvaluationCompleted)
                    .Then(ctx =>
                    {
                       if (!ctx.Message.Success) throw new Exception(ctx.Message.ErrorMessage);

                       var result = Deserialize<ACHWorkflowResult>(ctx.Saga.SerializedResult);
                       result.Evaluations = ctx.Message.Evaluations;
                       result.Success = true;
                       ctx.Saga.SerializedResult = JsonSerializer.Serialize(result);
                       ctx.Saga.CurrentStepIndex++;
                    })
                )
            );
        }

        public T Deserialize<T>(string json) => JsonSerializer.Deserialize<T>(json);

        // Helper for condition
        public bool IsStepType(ExperimentState instance, string typeFragment)
        {
            var config = JsonSerializer.Deserialize<ExperimentConfiguration>(instance.SerializedConfiguration);
            if (instance.CurrentStepIndex >= config.ACHSteps.Count()) return false;
            
            var step = config.ACHSteps[instance.CurrentStepIndex];
            return step.Name.ToLowerInvariant().Contains(typeFragment);
        }

        public bool IsComplete(ExperimentState instance)
        {
            var config = JsonSerializer.Deserialize<ExperimentConfiguration>(instance.SerializedConfiguration);
            return instance.CurrentStepIndex >= config.ACHSteps.Count();
        }

        public ACHStepConfiguration GetCurrentStep(ExperimentState instance)
        {
            var config = JsonSerializer.Deserialize<ExperimentConfiguration>(instance.SerializedConfiguration);
            return config.ACHSteps[instance.CurrentStepIndex];
        }

        public object CreateCommand(ExperimentState instance, ACHStepConfiguration step) 
        {
             var input = JsonSerializer.Deserialize<OrchestrationPromptInput>(instance.SerializedInput);
             var stepContext = new StepExecutionContext 
             { 
                 ExperimentId = instance.CorrelationId, 
                 StepExecutionId = Guid.NewGuid(),
                 AgentConfigurationIds = step.AgentConfigurations != null ? new Dictionary<string, Guid>() : null
             };
             
             return new 
             { 
                  ExperimentId = instance.CorrelationId,
                  StepExecutionId = stepContext.StepExecutionId,
                  Input = input,
                  Configuration = step,
                  StepContext = stepContext,
                  HypothesisStepExecutionId = instance.RefinedHypothesisStepExecutionId ?? instance.HypothesisStepExecutionId,
                  EvidenceStepExecutionId = instance.EvidenceStepExecutionId
             };
        }


        private EventActivityBinder<ExperimentState, T> DispatchStep<T>(EventActivityBinder<ExperimentState, T> binder) where T : class
        {
            return binder
                .IfElse(ctx => IsComplete(ctx.Saga),
                    x => x.ThenAsync(async ctx => {
                         _logger.LogInformation("Experiment {Id} Complete.", ctx.Saga.CorrelationId);
                         var res = Deserialize<ACHWorkflowResult>(ctx.Saga.SerializedResult);
                         await ctx.Publish<IExperimentCompleted>(new { ExperimentId = ctx.Saga.CorrelationId, Result = res, Timestamp = DateTime.UtcNow });
                    }).TransitionTo(Completed),
                    started => started
                        .If(ctx => IsStepType(ctx.Saga, "brainstorming"),
                            b => b.ThenAsync(ctx => ctx.Publish<IBrainstormingRequested>(CreateCommand(ctx.Saga, GetCurrentStep(ctx.Saga))))
                                  .TransitionTo(Brainstorming))
                        .If(ctx => IsStepType(ctx.Saga, "refinement"),
                            r => r.ThenAsync(ctx => ctx.Publish<IHypothesisRefinementRequested>(CreateCommand(ctx.Saga, GetCurrentStep(ctx.Saga))))
                                  .TransitionTo(Refining))
                        .If(ctx => IsStepType(ctx.Saga, "extraction"),
                            x => x.ThenAsync(ctx => ctx.Publish<IEvidenceExtractionRequested>(CreateCommand(ctx.Saga, GetCurrentStep(ctx.Saga))))
                                  .TransitionTo(Extracting))
                        .If(ctx => IsStepType(ctx.Saga, "evaluation"),
                            e => e.ThenAsync(ctx => ctx.Publish<IEvidenceEvaluationRequested>(CreateCommand(ctx.Saga, GetCurrentStep(ctx.Saga))))
                                  .TransitionTo(Evaluating))
                );
        }

    }
}
