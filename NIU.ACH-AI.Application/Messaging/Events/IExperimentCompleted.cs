using System;
using NIU.ACH_AI.Application.DTOs;

namespace NIU.ACH_AI.Application.Messaging.Events
{
    /// <summary>
    /// Signals that an ACH Experiment Saga has finished successfully.
    /// </summary>
    public interface IExperimentCompleted
    {
        Guid ExperimentId { get; }
        ACHWorkflowResult Result { get; }
        DateTime Timestamp { get; }
    }
}
