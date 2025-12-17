CREATE TABLE [dbo].[AGENT_RESPONSES]
(
	[agent_response_id] UNIQUEIDENTIFIER NOT NULL PRIMARY KEY, 
    [agent_configuration_id] UNIQUEIDENTIFIER NOT NULL, 
    [agent_name] NVARCHAR(50) NOT NULL, 
    [output_token_count] INT NULL, 
    [content_length] INT NULL, 
    [content] NVARCHAR(MAX) NULL, 
    [turn_number] INT NULL, 
    [response_duration] DECIMAL(10, 6) NULL
)
