using NIU.ACH_AI.Domain.Entities;

namespace NIU.ACH_AI.Application.DTOs
{
    /// <summary>
    /// Wrapper class for structured output - OpenAI requires top-level object, not array
    /// </summary>
    public class EvidenceResult
    {
        public List<Evidence> Evidence { get; set; } = new List<Evidence>();

        public override string ToString()
        {
            // Neatly format the evidence list for display
            return string.Join(Environment.NewLine + Environment.NewLine, Evidence.Select((e, index) =>
                $"{index + 1}. {e.ToString()}"));
        }
    }
}
