-- Hypotheses are possible answers to the Key Intelligence Question
-- Generated during the hypothesis brainstorming step of ACH
CREATE TABLE [dbo].[HYPOTHESES]
(
	[hypothesis_id] UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
	[step_execution_id] UNIQUEIDENTIFIER NOT NULL,  -- Step execution that generated this hypothesis
	[short_title] NVARCHAR(200) NOT NULL,  -- Brief summary title
	[hypothesis_text] NVARCHAR(MAX) NOT NULL,  -- Full hypothesis statement
	[is_refined] BIT NOT NULL DEFAULT 0,  -- Whether this hypothesis passed refinement step
	[created_at] DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),

	-- Foreign key constraints
	CONSTRAINT [FK_HYPOTHESES_STEP_EXECUTIONS]
	    FOREIGN KEY ([step_execution_id]) REFERENCES [STEP_EXECUTIONS]([step_execution_id])
)
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
