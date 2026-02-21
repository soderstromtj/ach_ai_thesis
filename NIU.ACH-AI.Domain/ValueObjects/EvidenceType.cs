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
        /// An explicitly stated, verifiable event, action, or data point grounded in objective reality.
        /// </summary>
        VerifiableFact = 1,

        /// <summary>
        /// A premise or unverified condition that the text treats as true in order to build its narrative or argument.
        /// </summary>
        StatedAssumption = 2,

        /// <summary>
        /// An analytical judgment, forecast, or evaluation formally attributed to a credentialed expert, organization, or authority.
        /// </summary>
        ExpertAssessment = 3,

        /// <summary>
        /// A high-confidence factual conclusion derived implicitly by cross-referencing two or more explicit facts within the text.
        /// </summary>
        InferredFact = 4,

        /// <summary>
        /// An unverified assertion or allegation made by a specific source, witness, or spokesperson.
        /// </summary>
        AttributedClaim = 5,

        /// <summary>
        /// A stated plan, threat, promise, or directive regarding a future action.
        /// </summary>
        StatedIntent = 6
    }
}
