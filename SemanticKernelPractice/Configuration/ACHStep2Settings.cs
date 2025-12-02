namespace SemanticKernelPractice.Configuration
{
    /// <summary>
    /// Configuration for ACH Step 2 experiments
    /// </summary>
    public class ACHStep2Settings
    {
        public ACHStep2ExperimentSettings[] Experiments { get; set; } = Array.Empty<ACHStep2ExperimentSettings>();
    }
}
