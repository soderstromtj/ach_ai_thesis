-- ACH_STEPS defines the steps in the Analysis of Competing Hypotheses methodology
-- primary_ach_step groups substeps (e.g., 1a, 1b, 1c all have primary_ach_step=1)
CREATE TABLE [dbo].[ACH_STEPS]
(
	[ach_step_id] INT NOT NULL PRIMARY KEY,
    [step_name] NVARCHAR(100) NOT NULL,  -- Name of the ACH step
    [step_order] INT NOT NULL,  -- Execution order within the methodology
    [description] NVARCHAR(500) NOT NULL,  -- Description of what this step does
    [primary_ach_step] INT NOT NULL,  -- Groups substeps under main step. NOT a foreign key.
    CONSTRAINT [UQ_ACH_STEPS_step_name] UNIQUE ([step_name])
)
GO

-- Index for finding substeps grouped by primary step
CREATE NONCLUSTERED INDEX [IX_ACH_STEPS_primary_ach_step]
    ON [dbo].[ACH_STEPS]([primary_ach_step])
    INCLUDE ([step_name], [step_order]);
GO

-- Index for ordered queries
CREATE NONCLUSTERED INDEX [IX_ACH_STEPS_step_order]
    ON [dbo].[ACH_STEPS]([step_order]);
GO
