-- Agent configurations define which AI agents are used in step executions
-- Each configuration specifies the agent's instructions, model, and provider
CREATE TABLE [dbo].[AGENT_CONFIGURATIONS]
(
	[agent_configuration_id] UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
    [step_execution_id] UNIQUEIDENTIFIER NOT NULL,                  -- References STEP_EXECUTIONS (was ach_step_workflow_id)
    [agent_name] NVARCHAR(50) NOT NULL,                             -- Snapshot of agent name at configuration time
    [description] NVARCHAR(500) NOT NULL,
    [instructions] NVARCHAR(MAX) NOT NULL,                          -- System prompt/instructions for the agent
    [provider_id] UNIQUEIDENTIFIER NOT NULL,                        -- AI service provider (OpenAI, Azure, etc.)
    [model_id] UNIQUEIDENTIFIER NOT NULL,                           -- Specific model (gpt-4o-mini, gpt-5-mini, etc.)
    [created_at] DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    [updated_at] DATETIME2 NULL,

    -- Foreign key constraints
    CONSTRAINT [FK_AGENT_CONFIGURATIONS_STEP_EXECUTIONS]
        FOREIGN KEY ([step_execution_id]) REFERENCES [STEP_EXECUTIONS]([step_execution_id]),
    CONSTRAINT [FK_AGENT_CONFIGURATIONS_PROVIDERS]
        FOREIGN KEY ([provider_id]) REFERENCES [PROVIDERS]([provider_id]),
    CONSTRAINT [FK_AGENT_CONFIGURATIONS_MODELS]
        FOREIGN KEY ([model_id]) REFERENCES [MODELS]([model_id])
)
GO

-- Index for finding all agent configurations for a step execution
CREATE NONCLUSTERED INDEX [IX_AGENT_CONFIGURATIONS_step_execution_id]
    ON [dbo].[AGENT_CONFIGURATIONS]([step_execution_id])
    INCLUDE ([agent_name], [provider_id], [model_id]);
GO

-- Index for provider-based queries
CREATE NONCLUSTERED INDEX [IX_AGENT_CONFIGURATIONS_provider_id]
    ON [dbo].[AGENT_CONFIGURATIONS]([provider_id]);
GO

-- Index for model-based queries
CREATE NONCLUSTERED INDEX [IX_AGENT_CONFIGURATIONS_model_id]
    ON [dbo].[AGENT_CONFIGURATIONS]([model_id]);
GO
