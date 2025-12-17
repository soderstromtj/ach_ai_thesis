CREATE TABLE [dbo].[ACH_STEP_WORKFLOW]
(
	[ach_step_workflow_id] UNIQUEIDENTIFIER NOT NULL PRIMARY KEY, 
    [experiment_id] UNIQUEIDENTIFIER NOT NULL, 
    [ach_step_id] INT NOT NULL, 
    [ach_step_name] NVARCHAR(100) NOT NULL,
    [description] NVARCHAR(500) NULL,
    [task_instructions] NVARCHAR(MAX) NULL,
    [orchestration_type_id] UNIQUEIDENTIFIER NULL,
    [datetime_start] DATETIME2 NULL, 
    [datetime_end] DATETIME2 NULL, 
    CONSTRAINT [FK_ACH_STEP_WORKFLOW_EXPERIMENTS] FOREIGN KEY ([experiment_id]) REFERENCES [EXPERIMENTS]([experiment_id]), 
    CONSTRAINT [FK_ACH_STEP_WORKFLOW_ACH_STEPS] FOREIGN KEY ([ach_step_id]) REFERENCES [ACH_STEPS]([ach_step_id]), 
    CONSTRAINT [FK_ACH_STEP_WORKFLOW_ORCHESTRATION_TYPES] FOREIGN KEY ([orchestration_type_id]) REFERENCES [ORCHESTRATION_TYPES]([orchestration_type_id])
)
