using System;
using System.Threading;
using System.Threading.Tasks;
using NIU.ACH_AI.Application.DTOs;

namespace NIU.ACH_AI.Application.Interfaces
{
    /// <summary>
    /// Service responsible for monitoring the progress and completion of an ACH workflow.
    /// Extracted to enforce the Single Responsibility Principle.
    /// </summary>
    public interface IExperimentMonitoringService
    {
        /// <summary>
        /// Polls the persistence layer for Saga completion logic.
        /// </summary>
        Task<ACHWorkflowResult> WaitForCompletionAsync(Guid experimentId, string experimentName, CancellationToken cancellationToken = default);
    }
}
