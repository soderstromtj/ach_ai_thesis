-- ORCHESTRATION_TYPES: Defines how agents are orchestrated in step executions
-- Examples: "Sequential", "Parallel", "RoundRobin", "Consensus"
CREATE TABLE [dbo].[ORCHESTRATION_TYPES]
(
	[orchestration_type_id] UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
    [description] NVARCHAR(50) NOT NULL  -- e.g., "Sequential", "Parallel"
)
GO
