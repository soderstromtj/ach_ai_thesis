namespace SemanticKernelPractice.Configuration
{
    public class OllamaSettings
    {
        public string Endpoint { get; set; } = "http://localhost:11434";
        public string ModelId { get; set; } = "llama2";
        public string? ServiceId { get; set; } = null;
    }
}
