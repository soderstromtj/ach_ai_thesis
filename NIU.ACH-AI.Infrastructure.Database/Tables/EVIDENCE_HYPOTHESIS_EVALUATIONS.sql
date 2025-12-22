-- Evidence-hypothesis evaluations form the core of ACH analysis
-- Each record represents how a piece of evidence relates to a specific hypothesis
CREATE TABLE [dbo].[EVIDENCE_HYPOTHESIS_EVALUATIONS]
(
	[evidence_hypothesis_evaluation_id] UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
    [step_execution_id] UNIQUEIDENTIFIER NOT NULL,  -- Step execution that created this evaluation
    [hypothesis_id] UNIQUEIDENTIFIER NOT NULL,  -- Hypothesis being evaluated
    [evidence_id] UNIQUEIDENTIFIER NOT NULL,  -- Evidence being evaluated
    [evaluation_score_id] INT NOT NULL,  -- Score (Consistent, Inconsistent, Neutral, etc.)
    [rationale] NVARCHAR(MAX) NULL,  -- Explanation of the evaluation
    [confidence_score] DECIMAL(5, 4) NULL,  -- Numeric confidence (0.0000 to 1.0000)
    [confidence_rationale] NVARCHAR(MAX) NULL,  -- Explanation of confidence level
    [created_at] DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),

    -- Foreign key constraints
    CONSTRAINT [FK_EHE_STEP_EXECUTION]
        FOREIGN KEY ([step_execution_id]) REFERENCES [STEP_EXECUTIONS]([step_execution_id]),
    CONSTRAINT [FK_EHE_HYPOTHESIS]
        FOREIGN KEY ([hypothesis_id]) REFERENCES [HYPOTHESES]([hypothesis_id]),
    CONSTRAINT [FK_EHE_EVIDENCE]
        FOREIGN KEY ([evidence_id]) REFERENCES [EVIDENCE]([evidence_id]),
    CONSTRAINT [FK_EHE_EVALUATION_SCORE]
        FOREIGN KEY ([evaluation_score_id]) REFERENCES [EVALUATION_SCORES]([evaluation_score_id])
)
GO

-- Index for finding all evaluations for a hypothesis (ACH matrix view)
CREATE NONCLUSTERED INDEX [IX_EHE_hypothesis_id]
    ON [dbo].[EVIDENCE_HYPOTHESIS_EVALUATIONS]([hypothesis_id])
    INCLUDE ([evidence_id], [evaluation_score_id], [confidence_score]);
GO

-- Index for finding all evaluations for a piece of evidence
CREATE NONCLUSTERED INDEX [IX_EHE_evidence_id]
    ON [dbo].[EVIDENCE_HYPOTHESIS_EVALUATIONS]([evidence_id])
    INCLUDE ([hypothesis_id], [evaluation_score_id]);
GO

-- Index for finding all evaluations from a step execution
CREATE NONCLUSTERED INDEX [IX_EHE_step_execution_id]
    ON [dbo].[EVIDENCE_HYPOTHESIS_EVALUATIONS]([step_execution_id])
    INCLUDE ([hypothesis_id], [evidence_id], [evaluation_score_id]);
GO

-- Index for filtering by evaluation score
CREATE NONCLUSTERED INDEX [IX_EHE_evaluation_score_id]
    ON [dbo].[EVIDENCE_HYPOTHESIS_EVALUATIONS]([evaluation_score_id])
    INCLUDE ([hypothesis_id], [evidence_id]);
GO
