using NIU.ACH_AI.Application.Configuration;

namespace NIU.ACH_AI.Application.Interfaces
{
    /// <summary>
    /// Service responsible for initializing an experiment before coordination begins.
    /// Extracted to enforce the Single Responsibility Principle.
    /// </summary>
    public interface IExperimentInitializationService
    {
        /// <summary>
        /// Initializes the scenario and experiment, returning the assigned Experiment Id.
        /// </summary>
        Task<Guid> InitializeExperimentAsync(ExperimentConfiguration experimentConfig, CancellationToken cancellationToken = default);
    }
}
