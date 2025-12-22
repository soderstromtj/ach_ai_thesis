-- Step executions track individual ACH step runs within an experiment
-- Each execution includes configuration snapshot, timing, and status information
CREATE TABLE [dbo].[STEP_EXECUTIONS]
(
	[step_execution_id] UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
    [experiment_id] UNIQUEIDENTIFIER NOT NULL,                      -- Parent experiment
    [ach_step_id] INT NOT NULL,                                     -- Reference to ACH methodology step
    [ach_step_name] NVARCHAR(100) NOT NULL,                         -- Snapshot of step name at execution time
    [description] NVARCHAR(500) NULL,                               -- Description of this specific execution
    [task_instructions] NVARCHAR(MAX) NULL,                         -- Instructions/prompt for agents in this step
    [orchestration_type_id] UNIQUEIDENTIFIER NULL,                  -- How agents are orchestrated (sequential, parallel, etc.)
    [datetime_start] DATETIME2 NULL,                                -- When execution started
    [datetime_end] DATETIME2 NULL,                                  -- When execution completed
    [execution_status] NVARCHAR(50) NULL,                           -- NotStarted, Running, Completed, Failed, Cancelled
    [error_message] NVARCHAR(MAX) NULL,                             -- Full error message if execution failed
    [error_type] NVARCHAR(100) NULL,                                -- Categorized error type (RateLimitError, TimeoutError, etc.)
    [retry_count] INT NULL DEFAULT 0,                               -- Number of retry attempts

    -- Foreign key constraints
    CONSTRAINT [FK_STEP_EXECUTIONS_EXPERIMENTS]
        FOREIGN KEY ([experiment_id]) REFERENCES [EXPERIMENTS]([experiment_id]),
    CONSTRAINT [FK_STEP_EXECUTIONS_ACH_STEPS]
        FOREIGN KEY ([ach_step_id]) REFERENCES [ACH_STEPS]([ach_step_id]),
    CONSTRAINT [FK_STEP_EXECUTIONS_ORCHESTRATION_TYPES]
        FOREIGN KEY ([orchestration_type_id]) REFERENCES [ORCHESTRATION_TYPES]([orchestration_type_id]),

    -- Check constraint for valid execution status values
    CONSTRAINT [CK_STEP_EXECUTIONS_execution_status]
        CHECK ([execution_status] IN ('NotStarted', 'Running', 'Completed', 'Failed', 'Cancelled'))
)
GO

-- Index for finding all step executions in an experiment (most common query)
CREATE NONCLUSTERED INDEX [IX_STEP_EXECUTIONS_experiment_id]
    ON [dbo].[STEP_EXECUTIONS]([experiment_id])
    INCLUDE ([ach_step_id], [ach_step_name], [execution_status], [datetime_start], [datetime_end]);
GO

-- Index for filtering by ACH step type
CREATE NONCLUSTERED INDEX [IX_STEP_EXECUTIONS_ach_step_id]
    ON [dbo].[STEP_EXECUTIONS]([ach_step_id])
    INCLUDE ([experiment_id], [execution_status]);
GO

-- Index for execution status queries (finding failed steps, etc.)
CREATE NONCLUSTERED INDEX [IX_STEP_EXECUTIONS_execution_status]
    ON [dbo].[STEP_EXECUTIONS]([execution_status])
    INCLUDE ([experiment_id], [ach_step_name], [error_type]);
GO

-- Index for chronological queries
CREATE NONCLUSTERED INDEX [IX_STEP_EXECUTIONS_datetime_start]
    ON [dbo].[STEP_EXECUTIONS]([datetime_start])
    INCLUDE ([experiment_id], [execution_status]);
GO
