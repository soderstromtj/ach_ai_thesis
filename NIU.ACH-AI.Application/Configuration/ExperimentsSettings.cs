namespace NIU.ACH_AI.Application.Configuration
{
    /// <summary>
    /// Acts as the root binding target for the application settings hierarchy.
    /// </summary>
    /// <remarks>
    /// Used by the dependency injection framework to extract and instantiate the array of defined analytical tests from the JSON configuration.
    /// </remarks>
    public class ExperimentsSettings
    {
        /// <summary>
        /// Gets or sets the collection of predefined analytical tests available for execution.
        /// </summary>
        public ExperimentConfiguration[] Experiments { get; set; } = Array.Empty<ExperimentConfiguration>();
    }
}
