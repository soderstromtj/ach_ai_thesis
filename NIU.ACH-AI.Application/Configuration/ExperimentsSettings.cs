namespace NIU.ACH_AI.Application.Configuration
{
    /// <summary>
    /// Root configuration class for binding the Experiments section from appsettings.json.
    /// Contains an array of experiment configurations, each with multiple ACH steps.
    /// </summary>
    public class ExperimentsSettings
    {
        /// <summary>
        /// Array of experiment configurations
        /// </summary>
        public ExperimentConfiguration[] Experiments { get; set; } = Array.Empty<ExperimentConfiguration>();
    }
}
