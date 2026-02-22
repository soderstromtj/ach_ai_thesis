-- Step executions track individual ACH step runs within an experiment
-- Each execution includes configuration snapshot, timing, and status information
CREATE TABLE [dbo].[STEP_EXECUTIONS] (
    [step_execution_id]     UNIQUEIDENTIFIER NOT NULL,
    [experiment_id]         UNIQUEIDENTIFIER NOT NULL,
    [ach_step_id]           INT              NOT NULL,
    [ach_step_name]         NVARCHAR (100)   NOT NULL,
    [description]           NVARCHAR (500)   NULL,
    [task_instructions]     NVARCHAR (MAX)   NULL,
    [orchestration_type_id] UNIQUEIDENTIFIER NULL,
    [datetime_start]        DATETIME2 (7)    NULL,
    [datetime_end]          DATETIME2 (7)    NULL,
    [execution_status]      NVARCHAR (50)    NULL,
    [error_message]         NVARCHAR (MAX)   NULL,
    [error_type]            NVARCHAR (100)   NULL,
    [retry_count]           INT              DEFAULT ((0)) NULL,
    PRIMARY KEY CLUSTERED ([step_execution_id] ASC),
    CONSTRAINT [CK_STEP_EXECUTIONS_execution_status] CHECK ([execution_status]='Cancelled' OR [execution_status]='Failed' OR [execution_status]='Completed' OR [execution_status]='Running' OR [execution_status]='NotStarted'),
    CONSTRAINT [FK_STEP_EXECUTIONS_ACH_STEPS] FOREIGN KEY ([ach_step_id]) REFERENCES [dbo].[ACH_STEPS] ([ach_step_id]),
    CONSTRAINT [FK_STEP_EXECUTIONS_EXPERIMENTS] FOREIGN KEY ([experiment_id]) REFERENCES [dbo].[EXPERIMENTS] ([experiment_id]),
    CONSTRAINT [FK_STEP_EXECUTIONS_ORCHESTRATION_TYPES] FOREIGN KEY ([orchestration_type_id]) REFERENCES [dbo].[ORCHESTRATION_TYPES] ([orchestration_type_id])
);


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
