using System;
using NIU.ACH_AI.Application.DTOs;

namespace NIU.ACH_AI.Application.Messaging.Events
{
    /// <summary>
    /// Event published when an ACH Experiment Saga completes successfully.
    /// </summary>
    public interface IExperimentCompleted
    {
        Guid ExperimentId { get; }
        ACHWorkflowResult Result { get; }
        DateTime Timestamp { get; }
    }
}
