using NIU.ACH_AI.Application.Configuration;
using NIU.ACH_AI.Application.DTOs;

namespace NIU.ACH_AI.Application.Interfaces
{
    public interface IWorkflowPersistence
    {
        Task<Guid> CreateScenarioAsync(string context, CancellationToken cancellationToken = default);
        Task<Guid> CreateExperimentAsync(
            ExperimentConfiguration configuration,
            Guid scenarioId,
            CancellationToken cancellationToken = default);
        Task<StepExecutionContext> CreateStepExecutionAsync(
            Guid experimentId,
            ACHStepConfiguration stepConfiguration,
            CancellationToken cancellationToken = default);
        Task UpdateStepExecutionStatusAsync(
            Guid stepExecutionId,
            string status,
            DateTime? start = null,
            DateTime? end = null,
            string? errorType = null,
            string? errorMessage = null,
            int? retryCount = null,
            CancellationToken cancellationToken = default);
    }
}
