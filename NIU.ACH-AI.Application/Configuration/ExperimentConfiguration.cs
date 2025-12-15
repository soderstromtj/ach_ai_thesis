using System.Text.Json.Serialization;

namespace NIU.ACH_AI.Application.Configuration
{
    /// <summary>
    /// Experiment configuration that can be deserialized from JSON and used at runtime.
    /// Represents a complete experiment containing multiple ACH steps.
    /// </summary>
    public class ExperimentConfiguration
    {
        /// <summary>
        /// Unique identifier for this experiment
        /// </summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// The name of the experiment
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Description of what this experiment tests or analyzes
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// The Key question this experiment aims to answer
        /// </summary>
        [JsonPropertyName("KeyIntelligenceQuestion")]
        public string KeyQuestion { get; set; } = string.Empty;

        /// <summary>
        /// The context or background information for this experiment
        /// </summary>
        public string Context { get; set; } = string.Empty;

        /// <summary>
        /// The ACH steps that comprise this experiment
        /// </summary>
        public ACHStepConfiguration[] ACHSteps { get; set; } = Array.Empty<ACHStepConfiguration>();
    }
}
