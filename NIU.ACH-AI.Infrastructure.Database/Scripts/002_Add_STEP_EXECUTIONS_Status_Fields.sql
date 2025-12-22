/*
    Migration: Add execution status and error tracking to STEP_EXECUTIONS
    Date: 2025-12-22
    Purpose: Track step execution lifecycle and capture error information for analysis

    New fields:
    - execution_status: Track lifecycle (NotStarted, Running, Completed, Failed, Cancelled)
    - error_message: Full error message/stack trace for debugging
    - error_type: Categorized error type for analysis (RateLimitError, TimeoutError, etc.)
    - retry_count: Number of retry attempts for this execution
*/

-- Add execution_status field
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.STEP_EXECUTIONS') AND name = 'execution_status')
BEGIN
    ALTER TABLE [dbo].[STEP_EXECUTIONS]
    ADD [execution_status] NVARCHAR(50) NULL;

    PRINT 'Added execution_status column to STEP_EXECUTIONS';
END
GO

-- Add error_message field
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.STEP_EXECUTIONS') AND name = 'error_message')
BEGIN
    ALTER TABLE [dbo].[STEP_EXECUTIONS]
    ADD [error_message] NVARCHAR(MAX) NULL;

    PRINT 'Added error_message column to STEP_EXECUTIONS';
END
GO

-- Add error_type field
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.STEP_EXECUTIONS') AND name = 'error_type')
BEGIN
    ALTER TABLE [dbo].[STEP_EXECUTIONS]
    ADD [error_type] NVARCHAR(100) NULL;

    PRINT 'Added error_type column to STEP_EXECUTIONS';
END
GO

-- Add retry_count field
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.STEP_EXECUTIONS') AND name = 'retry_count')
BEGIN
    ALTER TABLE [dbo].[STEP_EXECUTIONS]
    ADD [retry_count] INT NULL DEFAULT 0;

    PRINT 'Added retry_count column to STEP_EXECUTIONS';
END
GO

-- Add check constraint for valid execution_status values
IF NOT EXISTS (SELECT * FROM sys.check_constraints WHERE name = 'CK_STEP_EXECUTIONS_execution_status')
BEGIN
    ALTER TABLE [dbo].[STEP_EXECUTIONS]
    ADD CONSTRAINT [CK_STEP_EXECUTIONS_execution_status]
        CHECK ([execution_status] IN ('NotStarted', 'Running', 'Completed', 'Failed', 'Cancelled'));

    PRINT 'Added check constraint for execution_status values';
END
GO

PRINT 'Successfully added status and error tracking fields to STEP_EXECUTIONS';
GO
