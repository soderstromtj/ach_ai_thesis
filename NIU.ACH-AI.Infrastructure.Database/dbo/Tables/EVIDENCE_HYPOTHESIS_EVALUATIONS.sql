-- Evidence-hypothesis evaluations form the core of ACH analysis
-- Each record represents how a piece of evidence relates to a specific hypothesis
CREATE TABLE [dbo].[EVIDENCE_HYPOTHESIS_EVALUATIONS] (
    [evidence_hypothesis_evaluation_id] UNIQUEIDENTIFIER NOT NULL,
    [step_execution_id]                 UNIQUEIDENTIFIER NOT NULL,
    [hypothesis_id]                     UNIQUEIDENTIFIER NOT NULL,
    [evidence_id]                       UNIQUEIDENTIFIER NOT NULL,
    [evaluation_score_id]               INT              NOT NULL,
    [rationale]                         NVARCHAR (MAX)   NULL,
    [confidence_score]                  DECIMAL (5, 4)   NULL,
    [confidence_rationale]              NVARCHAR (MAX)   NULL,
    [created_at]                        DATETIME2 (7)    DEFAULT (sysutcdatetime()) NOT NULL,
    PRIMARY KEY CLUSTERED ([evidence_hypothesis_evaluation_id] ASC),
    CONSTRAINT [CK_EHE_confidence_score] CHECK ([confidence_score] IS NULL OR [confidence_score]>=(0.0000) AND [confidence_score]<=(1.0000)),
    CONSTRAINT [FK_EHE_EVALUATION_SCORE] FOREIGN KEY ([evaluation_score_id]) REFERENCES [dbo].[EVALUATION_SCORES] ([evaluation_score_id]),
    CONSTRAINT [FK_EHE_EVIDENCE] FOREIGN KEY ([evidence_id]) REFERENCES [dbo].[EVIDENCE] ([evidence_id]),
    CONSTRAINT [FK_EHE_HYPOTHESIS] FOREIGN KEY ([hypothesis_id]) REFERENCES [dbo].[HYPOTHESES] ([hypothesis_id]),
    CONSTRAINT [FK_EHE_STEP_EXECUTION] FOREIGN KEY ([step_execution_id]) REFERENCES [dbo].[STEP_EXECUTIONS] ([step_execution_id])
);


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
