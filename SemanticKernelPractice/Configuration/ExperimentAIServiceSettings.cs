namespace SemanticKernelPractice.Configuration
{
    /// <summary>
    /// AI Service settings at the experiment level - only specifies which provider to use.
    /// The actual provider configurations (API keys, endpoints, etc.) come from AIServiceSettings.
    /// </summary>
    public class ExperimentAIServiceSettings
    {
        public AIServiceProvider Provider { get; set; } = AIServiceProvider.OpenAI;
    }
}
