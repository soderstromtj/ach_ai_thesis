namespace SemanticKernelPractice.Models
{
    /// <summary>
    /// Wrapper class for structured output - OpenAI requires top-level object, not array
    /// </summary>
    public class EvidenceResult
    {
        public List<Evidence> Evidence { get; set; } = new List<Evidence>();
    }
}
