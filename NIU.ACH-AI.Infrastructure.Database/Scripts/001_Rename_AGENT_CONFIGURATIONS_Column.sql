/*
    Migration: Rename AGENT_CONFIGURATIONS.ach_step_workflow_id to step_execution_id
    Date: 2025-12-22
    Purpose: Clarify that this column references STEP_EXECUTIONS, not a separate workflow table

    This column references STEP_EXECUTIONS records. The original name "ach_step_workflow_id"
    was misleading. Renaming to "step_execution_id" makes the relationship clear.
*/

-- Step 1: Drop existing foreign key constraint
IF EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_AGENT_CONFIGURATIONS_STEP_EXECUTIONS')
BEGIN
    ALTER TABLE [dbo].[AGENT_CONFIGURATIONS]
    DROP CONSTRAINT [FK_AGENT_CONFIGURATIONS_STEP_EXECUTIONS];
END
GO

-- Step 2: Rename the column
EXEC sp_rename
    @objname = 'dbo.AGENT_CONFIGURATIONS.ach_step_workflow_id',
    @newname = 'step_execution_id',
    @objtype = 'COLUMN';
GO

-- Step 3: Re-create foreign key constraint with correct name
ALTER TABLE [dbo].[AGENT_CONFIGURATIONS]
ADD CONSTRAINT [FK_AGENT_CONFIGURATIONS_STEP_EXECUTIONS]
    FOREIGN KEY ([step_execution_id])
    REFERENCES [dbo].[STEP_EXECUTIONS]([step_execution_id]);
GO

PRINT 'Successfully renamed ach_step_workflow_id to step_execution_id in AGENT_CONFIGURATIONS';
GO
