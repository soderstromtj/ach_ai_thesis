using NIU.ACH_AI.Domain.Entities;

namespace NIU.ACH_AI.Application.DTOs
{
    /// <summary>
    /// Represents the result of an evidence extraction phase.
    /// </summary>
    /// <remarks>
    /// This wrapper class is used to structure the output for LLM providers (e.g., OpenAI) which often require a top-level object rather than a raw array.
    /// </remarks>
    public class EvidenceResult
    {
        /// <summary>
        /// Gets or sets the list of evidence items extracted.
        /// </summary>
        public List<Evidence> Evidence { get; set; } = new List<Evidence>();

        /// <summary>
        /// Returns a string that represents the current object.
        /// </summary>
        /// <returns>
        /// A formatted string listing all evidence items.
        /// </returns>
        public override string ToString()
        {
            // Neatly format the evidence list for display
            return string.Join(Environment.NewLine + Environment.NewLine, Evidence.Select((e, index) =>
                $"{index + 1}. {e.ToString()}"));
        }
    }
}
