
using NIU.ACH_AI.Domain.Entities;

namespace NIU.ACH_AI.Application.DTOs
{
    /// <summary>
    /// Represents the result of a hypothesis generation phase.
    /// </summary>
    /// <remarks>
    /// Wraps a list of hypotheses to provide a structured object for serialization/deserialization.
    /// </remarks>
    public class HypothesisResult
    {
        /// <summary>
        /// Gets or sets the list of generated hypotheses.
        /// </summary>
        public List<Hypothesis> Hypotheses { get; set; } = new List<Hypothesis>();
    }
}
