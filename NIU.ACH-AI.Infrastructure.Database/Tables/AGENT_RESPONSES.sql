CREATE TABLE [dbo].[AGENT_RESPONSES]
(
	[agent_response_id] UNIQUEIDENTIFIER NOT NULL PRIMARY KEY, 
    [step_execution_id] UNIQUEIDENTIFIER NOT NULL,
    [agent_configuration_id] UNIQUEIDENTIFIER NOT NULL, 
    [agent_name] NVARCHAR(50) NOT NULL,
    [input_token_count] INT NULL,
    [output_token_count] INT NULL, 
    [content_length] INT NULL, 
    [content] NVARCHAR(MAX) NULL, 
    [turn_number] INT NULL, 
    [response_duration] BIGINT NULL, 
    [created_at] DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(), 
    CONSTRAINT [FK_AGENT_RESPONSES_AGENT_CONFIGURATIONS] FOREIGN KEY ([agent_configuration_id]) REFERENCES [AGENT_CONFIGURATIONS]([agent_configuration_id]), 
    CONSTRAINT [FK_AGENT_RESPONSES_STEP_EXECUTIONS] FOREIGN KEY ([step_execution_id]) REFERENCES [STEP_EXECUTIONS]([step_execution_id])
)
