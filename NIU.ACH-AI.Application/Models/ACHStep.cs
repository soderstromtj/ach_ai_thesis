namespace NIU.ACHAI.Application.Models
{
    /// <summary>
    /// Represents the different steps in the Analysis of Competing Hypotheses (ACH) process.
    /// </summary>
    public enum ACHStep
    {
        /// <summary>
        /// Step 1a: Generate hypotheses for the intelligence question.
        /// </summary>
        HypothesisBrainstorming = 1,

        /// <summary>
        /// Step 1b: Refine and select the most plausible hypotheses.
        /// </summary>
        HypothesisRefinementSelection = 2,

        /// <summary>
        /// Step 3: Extract and identify evidence relevant to the hypotheses.
        /// </summary>
        EvidenceExtraction = 3,

        /// <summary>
        /// Step 4: Evaluate evidence against hypotheses (future implementation).
        /// </summary>
        EvidenceEvaluation = 4
    }
}
