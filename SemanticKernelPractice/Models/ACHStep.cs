namespace SemanticKernelPractice.Models
{
    /// <summary>
    /// Represents the different steps in the Analysis of Competing Hypotheses (ACH) process.
    /// </summary>
    public enum ACHStep
    {
        /// <summary>
        /// Step 1: Generate hypotheses for the intelligence question.
        /// </summary>
        HypothesisGeneration = 1,

        /// <summary>
        /// Step 2: Extract and identify evidence relevant to the hypotheses.
        /// </summary>
        EvidenceExtraction = 2,

        /// <summary>
        /// Step 3: Evaluate evidence against hypotheses (future implementation).
        /// </summary>
        EvidenceEvaluation = 3,

        /// <summary>
        /// Step 4: Refine hypotheses based on evidence (future implementation).
        /// </summary>
        HypothesisRefinement = 4,

        /// <summary>
        /// Step 5: Draw conclusions and identify intelligence gaps (future implementation).
        /// </summary>
        ConclusionDrawing = 5
    }
}
