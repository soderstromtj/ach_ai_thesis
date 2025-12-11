using System.Text.Json.Serialization;

namespace SemanticKernelPractice.Configuration
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
        /// The ACH steps that comprise this experiment
        /// </summary>
        public ACHStepConfiguration[] ACHSteps { get; set; } = Array.Empty<ACHStepConfiguration>();

        /// <summary>
        /// Global AI service provider configurations (injected at runtime, not from JSON)
        /// </summary>
        [JsonIgnore]
        public AIServiceSettings GlobalAIServiceSettings { get; set; } = new();
    }
}
