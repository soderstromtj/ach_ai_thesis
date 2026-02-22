-- Hypotheses are possible answers to the Key Intelligence Question
-- Generated during the hypothesis brainstorming step of ACH
CREATE TABLE [dbo].[HYPOTHESES] (
    [hypothesis_id]     UNIQUEIDENTIFIER NOT NULL,
    [step_execution_id] UNIQUEIDENTIFIER NOT NULL,
    [short_title]       NVARCHAR (MAX)   NOT NULL,
    [hypothesis_text]   NVARCHAR (MAX)   NOT NULL,
    [is_refined]        BIT              DEFAULT ((0)) NOT NULL,
    [created_at]        DATETIME2 (7)    DEFAULT (sysutcdatetime()) NOT NULL,
    PRIMARY KEY CLUSTERED ([hypothesis_id] ASC),
    CONSTRAINT [FK_HYPOTHESES_STEP_EXECUTIONS] FOREIGN KEY ([step_execution_id]) REFERENCES [dbo].[STEP_EXECUTIONS] ([step_execution_id])
);


GO

-- Index for finding all hypotheses from a step execution
CREATE NONCLUSTERED INDEX [IX_HYPOTHESES_step_execution_id]
    ON [dbo].[HYPOTHESES]([step_execution_id])
    INCLUDE ([short_title], [is_refined], [created_at]);
GO

-- Index for filtering refined vs unrefined hypotheses
CREATE NONCLUSTERED INDEX [IX_HYPOTHESES_is_refined]
    ON [dbo].[HYPOTHESES]([is_refined])
    INCLUDE ([step_execution_id], [short_title]);
GO
