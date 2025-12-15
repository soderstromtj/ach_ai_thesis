using Microsoft.Extensions.DependencyInjection;
using NIU.ACH_AI.Application.Interfaces;
using NIU.ACH_AI.Domain.Entities;
using NIU.ACH_AI.Domain.ValueObjects;

namespace NIU.ACH_AI.Infrastructure.AI.Services
{
    /// <summary>
    /// Selects the appropriate orchestration factory based on the ACH step.
    /// </summary>
    public class OrchestrationFactorySelector : IOrchestrationFactorySelector
    {
        private readonly IServiceProvider _serviceProvider;

        public OrchestrationFactorySelector(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        }

        /// <summary>
        /// Gets the orchestration factory appropriate for the specified ACH step.
        /// </summary>
        /// <param name="step">The ACH step for which to get the factory.</param>
        /// <returns>The orchestration factory instance for the specified step.</returns>
        /// <exception cref="NotSupportedException">Thrown when the specified ACH step is not supported.</exception>
        public object GetFactory(ACHStep step)
        {
            return step switch
            {
                ACHStep.HypothesisBrainstorming => _serviceProvider.GetRequiredService<IOrchestrationFactory<List<Hypothesis>>>(),
                ACHStep.HypothesisRefinementSelection => _serviceProvider.GetRequiredService<IOrchestrationFactory<List<Evidence>>>(),
                ACHStep.EvidenceExtraction => throw new NotSupportedException($"ACH Step {step} is not yet implemented."),
                ACHStep.EvidenceEvaluation => throw new NotSupportedException($"ACH Step {step} is not yet implemented."),
                _ => throw new NotSupportedException($"Unknown ACH step: {step}")
            };
        }

        /// <summary>
        /// Gets the result type for the specified ACH step.
        /// </summary>
        /// <param name="step">The ACH step.</param>
        /// <returns>The Type of the result produced by the factory for this step.</returns>
        public Type GetResultType(ACHStep step)
        {
            return step switch
            {
                ACHStep.HypothesisBrainstorming => typeof(List<Hypothesis>),
                ACHStep.HypothesisRefinementSelection => typeof(List<Evidence>),
                ACHStep.EvidenceExtraction => throw new NotSupportedException($"ACH Step {step} is not yet implemented."),
                ACHStep.EvidenceEvaluation => throw new NotSupportedException($"ACH Step {step} is not yet implemented."),
                _ => throw new NotSupportedException($"Unknown ACH step: {step}")
            };
        }
    }
}
