using System;
using NIU.ACH_AI.Application.Configuration;

namespace NIU.ACH_AI.Application.Messaging.Events
{
    /// <summary>
    /// Triggers the start of a new ACH experiment workflow.
    /// </summary>
    public interface IExperimentStarted
    {
        Guid ExperimentId { get; }
        ExperimentConfiguration Configuration { get; }
        DateTime Timestamp { get; }
    }
}
