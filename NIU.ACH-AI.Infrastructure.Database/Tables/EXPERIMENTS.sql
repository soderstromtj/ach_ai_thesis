-- Experiments represent complete ACH analysis runs
-- Each experiment analyzes a scenario using a specific configuration of steps and agents
CREATE TABLE [dbo].[EXPERIMENTS]
(
	[experiment_id] UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
    [experiment_name] NVARCHAR(50) NOT NULL,        -- Human-readable experiment name
    [description] NVARCHAR(500) NULL,               -- Detailed description of experiment purpose
    [kiq] NVARCHAR(255) NOT NULL,                   -- Key Intelligence Question being analyzed
    [scenario_id] UNIQUEIDENTIFIER NOT NULL,        -- Scenario/context for this experiment
    [created_at] DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),

    -- Foreign key constraints
    CONSTRAINT [FK_EXPERIMENTS_SCENARIOS]
        FOREIGN KEY ([scenario_id]) REFERENCES [SCENARIOS]([scenario_id])
)
GO

-- Index for finding experiments by scenario
CREATE NONCLUSTERED INDEX [IX_EXPERIMENTS_scenario_id]
    ON [dbo].[EXPERIMENTS]([scenario_id])
    INCLUDE ([experiment_name], [kiq], [created_at]);
GO

-- Index for chronological queries and reporting
CREATE NONCLUSTERED INDEX [IX_EXPERIMENTS_created_at]
    ON [dbo].[EXPERIMENTS]([created_at])
    INCLUDE ([experiment_name], [scenario_id]);
GO
