namespace NIU.ACH_AI.Domain.Entities
{
    /// <summary>
    /// Represents a proposed explanation or theory that is evaluated against evidence.
    /// </summary>
    public class Hypothesis
    {
        /// <summary>
        /// Gets or sets the unique identifier for the hypothesis.
        /// </summary>
        public Guid HypothesisId { get; set; }

        /// <summary>
        /// Gets or sets a short, descriptive title for the hypothesis.
        /// </summary>
        public string ShortTitle { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the full text description of the hypothesis.
        /// </summary>
        public string HypothesisText { get; set; } = string.Empty;

        /// <summary>
        /// Returns a string that represents the current object.
        /// </summary>
        /// <returns>
        /// A string containing the short title and hypothesis text.
        /// </returns>
        public override string ToString()
        {
            return $"{ShortTitle}. {HypothesisText}";
        }
    }
}
