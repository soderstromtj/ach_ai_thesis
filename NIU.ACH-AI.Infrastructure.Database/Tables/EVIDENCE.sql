CREATE TABLE [dbo].[EVIDENCE]
(
	[evidence_id] UNIQUEIDENTIFIER NOT NULL PRIMARY KEY, 
    [step_execution_id] UNIQUEIDENTIFIER NOT NULL, 
    [claim] NVARCHAR(MAX) NOT NULL, 
    [reference_snippet] NVARCHAR(MAX) NULL, 
    [evidence_type_id] INT NOT NULL, 
    [notes] NVARCHAR(MAX) NULL,
    [created_at] DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(), 
    CONSTRAINT [FK_EVIDENCE_STEP_EXECUTIONS] FOREIGN KEY ([step_execution_id]) REFERENCES [STEP_EXECUTIONS]([step_execution_id]),
    CONSTRAINT [FK_EVIDENCE_EVIDENCE_TYPES] FOREIGN KEY ([evidence_type_id]) REFERENCES [EVIDENCE_TYPES]([evidence_type_id])

)
