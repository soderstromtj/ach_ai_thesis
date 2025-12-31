using System.Text.Json.Serialization;

namespace NIU.ACH_AI.Domain.ValueObjects
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    /// <summary>
    /// Represents the degree of consistency between a piece of evidence and a hypothesis.
    /// </summary>
    public enum EvaluationScore
    {
        /// <summary>
        /// The evidence strongly supports the hypothesis.
        /// </summary>
        VeryConsistent = 2,

        /// <summary>
        /// The evidence supports the hypothesis.
        /// </summary>
        Consistent = 1,

        /// <summary>
        /// The evidence neither supports nor refutes the hypothesis.
        /// </summary>
        Neutral = 0,

        /// <summary>
        /// The evidence contradicts the hypothesis.
        /// </summary>
        Inconsistent = -1,

        /// <summary>
        /// The evidence strongly contradicts the hypothesis.
        /// </summary>
        VeryInconsistent = -2
    }
}
