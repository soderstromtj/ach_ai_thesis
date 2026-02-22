-- Experiments represent complete ACH analysis runs
-- Each experiment analyzes a scenario using a specific configuration of steps and agents
CREATE TABLE [dbo].[EXPERIMENTS] (
    [experiment_id]   UNIQUEIDENTIFIER NOT NULL,
    [experiment_name] NVARCHAR (50)    NOT NULL,
    [description]     NVARCHAR (500)   NULL,
    [key_question]    NVARCHAR (255)   NOT NULL,
    [scenario_id]     UNIQUEIDENTIFIER NOT NULL,
    [created_at]      DATETIME2 (7)    DEFAULT (sysutcdatetime()) NOT NULL,
    PRIMARY KEY CLUSTERED ([experiment_id] ASC),
    CONSTRAINT [FK_EXPERIMENTS_SCENARIOS] FOREIGN KEY ([scenario_id]) REFERENCES [dbo].[SCENARIOS] ([scenario_id])
);


GO

-- Index for finding experiments by scenario
CREATE NONCLUSTERED INDEX [IX_EXPERIMENTS_scenario_id]
    ON [dbo].[EXPERIMENTS]([scenario_id] ASC)
    INCLUDE([experiment_name], [key_question], [created_at]);


GO

-- Index for chronological queries and reporting
CREATE NONCLUSTERED INDEX [IX_EXPERIMENTS_created_at]
    ON [dbo].[EXPERIMENTS]([created_at])
    INCLUDE ([experiment_name], [scenario_id]);
GO
