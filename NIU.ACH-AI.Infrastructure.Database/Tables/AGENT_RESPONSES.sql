-- Agent responses capture outputs from AI agents during step executions
-- Includes token counts, response content, and performance metrics
CREATE TABLE [dbo].[AGENT_RESPONSES] (
    [agent_response_id]               UNIQUEIDENTIFIER NOT NULL,
    [step_execution_id]               UNIQUEIDENTIFIER NOT NULL,
    [agent_configuration_id]          UNIQUEIDENTIFIER NOT NULL,
    [agent_name]                      NVARCHAR (50)    NOT NULL,
    [input_token_count]               INT              NULL,
    [output_token_count]              INT              NULL,
    [content_length]                  INT              NULL,
    [content]                         NVARCHAR (MAX)   NULL,
    [turn_number]                     INT              NULL,
    [response_duration]               BIGINT           NULL,
    [created_at]                      DATETIME2 (7)    DEFAULT (sysutcdatetime()) NOT NULL,
    [completion_id]                   NVARCHAR (100)   NULL,
    [reasoning_token_count]           INT              NULL,
    [output_audio_token_count]        INT              NULL,
    [accepted_prediction_token_count] INT              NULL,
    [rejected_prediction_token_count] INT              NULL,
    [input_audio_token_count]         INT              NULL,
    [cached_input_token_count]        INT              NULL,
    [finished_at]                     DATETIME2 (7)    DEFAULT (getutcdate()) NOT NULL,
    PRIMARY KEY CLUSTERED ([agent_response_id] ASC),
    CONSTRAINT [CK_AGENT_RESPONSES_accepted_prediction_token_count] CHECK ([accepted_prediction_token_count] IS NULL OR [accepted_prediction_token_count]>=(0)),
    CONSTRAINT [CK_AGENT_RESPONSES_cached_input_token_count] CHECK ([cached_input_token_count] IS NULL OR [cached_input_token_count]>=(0)),
    CONSTRAINT [CK_AGENT_RESPONSES_content_length] CHECK ([content_length] IS NULL OR [content_length]>=(0)),
    CONSTRAINT [CK_AGENT_RESPONSES_input_audio_token_count] CHECK ([input_audio_token_count] IS NULL OR [input_audio_token_count]>=(0)),
    CONSTRAINT [CK_AGENT_RESPONSES_input_token_count] CHECK ([input_token_count] IS NULL OR [input_token_count]>=(0)),
    CONSTRAINT [CK_AGENT_RESPONSES_output_audio_token_count] CHECK ([output_audio_token_count] IS NULL OR [output_audio_token_count]>=(0)),
    CONSTRAINT [CK_AGENT_RESPONSES_output_token_count] CHECK ([output_token_count] IS NULL OR [output_token_count]>=(0)),
    CONSTRAINT [CK_AGENT_RESPONSES_reasoning_token_count] CHECK ([reasoning_token_count] IS NULL OR [reasoning_token_count]>=(0)),
    CONSTRAINT [CK_AGENT_RESPONSES_rejected_prediction_token_count] CHECK ([rejected_prediction_token_count] IS NULL OR [rejected_prediction_token_count]>=(0)),
    CONSTRAINT [CK_AGENT_RESPONSES_response_duration] CHECK ([response_duration] IS NULL OR [response_duration]>=(0)),
    CONSTRAINT [CK_AGENT_RESPONSES_turn_number] CHECK ([turn_number] IS NULL OR [turn_number]>(0)),
    CONSTRAINT [FK_AGENT_RESPONSES_AGENT_CONFIGURATIONS] FOREIGN KEY ([agent_configuration_id]) REFERENCES [dbo].[AGENT_CONFIGURATIONS] ([agent_configuration_id]),
    CONSTRAINT [FK_AGENT_RESPONSES_STEP_EXECUTIONS] FOREIGN KEY ([step_execution_id]) REFERENCES [dbo].[STEP_EXECUTIONS] ([step_execution_id])
);


GO

-- Index for finding all responses for a step execution (most common query)
CREATE NONCLUSTERED INDEX [IX_AGENT_RESPONSES_step_execution_id]
    ON [dbo].[AGENT_RESPONSES]([step_execution_id])
    INCLUDE ([agent_name], [input_token_count], [output_token_count], [response_duration]);
GO

-- Index for finding all responses from a specific agent configuration
CREATE NONCLUSTERED INDEX [IX_AGENT_RESPONSES_agent_configuration_id]
    ON [dbo].[AGENT_RESPONSES]([agent_configuration_id])
    INCLUDE ([input_token_count], [output_token_count], [created_at]);
GO

-- Index for time-series analysis and cost tracking
CREATE NONCLUSTERED INDEX [IX_AGENT_RESPONSES_created_at]
    ON [dbo].[AGENT_RESPONSES]([created_at])
    INCLUDE ([step_execution_id], [input_token_count], [output_token_count]);
GO
