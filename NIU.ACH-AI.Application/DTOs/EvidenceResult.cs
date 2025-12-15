using NIU.ACH_AI.Domain.Entities;

namespace NIU.ACH_AI.Application.DTOs
{
    /// <summary>
    /// Wrapper class for structured output - OpenAI requires top-level object, not array
    /// </summary>
    public class EvidenceResult
    {
        public List<Evidence> Evidence { get; set; } = new List<Evidence>();
    }
}
