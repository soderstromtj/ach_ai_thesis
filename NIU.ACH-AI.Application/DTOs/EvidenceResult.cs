using NIU.ACH_AI.Domain.Entities;

namespace NIU.ACH_AI.Application.DTOs
{
    /// <summary>
    /// Wraps the extracted factual points into a structured object.
    /// </summary>
    /// <remarks>
    /// Required by many LLM JSON configurations to ensure a valid top-level schema is parsed instead of a raw array.
    /// </remarks>
    public class EvidenceResult
    {
        /// <summary>
        /// Gets or sets the collection of factual points derived from the document context.
        /// </summary>
        public List<Evidence> Evidence { get; set; } = new List<Evidence>();

        /// <summary>
        /// Formats the evidence points with numerical bullets for readability.
        /// </summary>
        /// <returns>A multiline string of points.</returns>
        public override string ToString()
        {
            // Neatly format the evidence list for display
            return string.Join(Environment.NewLine + Environment.NewLine, Evidence.Select((e, index) =>
                $"{index + 1}. {e.ToString()}"));
        }
    }
}
