using System.Text.Json.Serialization;

namespace NIU.ACH_AI.Domain.ValueObjects
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    /// <summary>
    /// Categorizes the nature or source of a piece of evidence.
    /// </summary>
    public enum EvidenceType
    {
        /// <summary>
        /// A verifiable piece of information.
        /// </summary>
        Fact = 1,

        /// <summary>
        /// A premise that is accepted as true without proof.
        /// </summary>
        Assumption = 2,

        /// <summary>
        /// A judgment or conclusion provided by a subject matter expert.
        /// </summary>
        ExpertOpinion = 3
    }
}
