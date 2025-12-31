using NIU.ACH_AI.Domain.ValueObjects;

namespace NIU.ACH_AI.Domain.Entities
{
    /// <summary>
    /// Represents a piece of information or data that is used to support or refute a <see cref="Hypothesis"/>.
    /// </summary>
    public class Evidence
    {
        /// <summary>
        /// Gets or sets the unique identifier for the evidence.
        /// </summary>
        public Guid EvidenceId { get; set; }

        /// <summary>
        /// Gets or sets the core assertion or claim made by this evidence.
        /// </summary>
        /// <remarks>
        /// This should be a concise statement of what the evidence suggests.
        /// </remarks>
        public string Claim { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the original text or source snippet from which the evidence was extracted.
        /// </summary>
        /// <value>
        /// The raw text snippet, or <c>null</c> if not available.
        /// </value>
        public string? ReferenceSnippet { get; set; }

        /// <summary>
        /// Gets or sets the category of the evidence.
        /// </summary>
        public EvidenceType Type { get; set; }

        /// <summary>
        /// Gets or sets additional notes or context regarding the evidence.
        /// </summary>
        public string Notes { get; set; } = string.Empty;

        /// <summary>
        /// Returns a string that represents the current object.
        /// </summary>
        /// <returns>
        /// A string containing the ID, claim, type, notes, and reference snippet.
        /// </returns>
        public override string ToString()
        {
            return $"EvidenceId: {EvidenceId}\n" +
                   $"Claim: {Claim}\n" +
                   $"Type: {Type}\n" +
                   $"Notes: {Notes ?? "N/A"}\n" +
                   $"ReferenceSnippet: {ReferenceSnippet ?? "N/A"}";
        }
    }
}
