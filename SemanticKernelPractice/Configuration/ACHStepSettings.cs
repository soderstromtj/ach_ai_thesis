namespace SemanticKernelPractice.Configuration
{
    /// <summary>
    /// Configuration for ACH experiments for any step
    /// </summary>
    public class ACHStepSettings
    {
        public ExperimentConfiguration[] Experiments { get; set; } = Array.Empty<ExperimentConfiguration>();
    }
}
