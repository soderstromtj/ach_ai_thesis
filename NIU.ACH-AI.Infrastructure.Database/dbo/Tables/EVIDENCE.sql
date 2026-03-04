-- Evidence stores facts and assumptions extracted from source material during ACH analysis
-- Each piece of evidence is categorized by type and linked to the step execution that produced it
CREATE TABLE [dbo].[EVIDENCE]
(
	[evidence_id] UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
    [step_execution_id] UNIQUEIDENTIFIER NOT NULL,          -- Step execution that extracted this evidence
    [claim] NVARCHAR(MAX) NOT NULL,                         -- The evidence claim or statement
    [reference_snippet] NVARCHAR(MAX) NULL,                 -- Verbatim quote from source material
    [evidence_type_id] INT NOT NULL,                        -- Type of evidence (Fact, Assumption, etc.)
    [notes] NVARCHAR(MAX) NULL,                             -- Additional context, credibility notes, or analyst comments
    [created_at] DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),

    -- Foreign key constraints
    CONSTRAINT [FK_EVIDENCE_STEP_EXECUTIONS]
        FOREIGN KEY ([step_execution_id]) REFERENCES [STEP_EXECUTIONS]([step_execution_id]),
    CONSTRAINT [FK_EVIDENCE_EVIDENCE_TYPES]
        FOREIGN KEY ([evidence_type_id]) REFERENCES [EVIDENCE_TYPES]([evidence_type_id])
)
GO

-- Index for finding all evidence from a step execution (most common query)
CREATE NONCLUSTERED INDEX [IX_EVIDENCE_step_execution_id]
    ON [dbo].[EVIDENCE]([step_execution_id])
    INCLUDE ([evidence_type_id], [created_at]);
GO

-- Index for filtering/grouping by evidence type
CREATE NONCLUSTERED INDEX [IX_EVIDENCE_evidence_type_id]
    ON [dbo].[EVIDENCE]([evidence_type_id])
    INCLUDE ([step_execution_id]);
GO
