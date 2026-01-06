using System;
using NIU.ACH_AI.Application.Configuration;

namespace NIU.ACH_AI.Application.Messaging.Events
{
    /// <summary>
    /// Event triggered to start a new ACH Experiment Saga.
    /// </summary>
    public interface IExperimentStarted
    {
        Guid ExperimentId { get; }
        ExperimentConfiguration Configuration { get; }
        DateTime Timestamp { get; }
    }
}
