ALTER TABLE [dbo].[AGENT_RESPONSES]
ADD [completion_id] NVARCHAR(100) NULL,
    [reasoning_token_count] INT NULL,
    [output_audio_token_count] INT NULL,
    [accepted_prediction_token_count] INT NULL,
    [rejected_prediction_token_count] INT NULL,
    [input_audio_token_count] INT NULL,
    [cached_input_token_count] INT NULL;
GO

ALTER TABLE [dbo].[AGENT_RESPONSES]
ADD CONSTRAINT [CK_AGENT_RESPONSES_reasoning_token_count]
    CHECK ([reasoning_token_count] IS NULL OR [reasoning_token_count] >= 0);
GO

ALTER TABLE [dbo].[AGENT_RESPONSES]
ADD CONSTRAINT [CK_AGENT_RESPONSES_output_audio_token_count]
    CHECK ([output_audio_token_count] IS NULL OR [output_audio_token_count] >= 0);
GO

ALTER TABLE [dbo].[AGENT_RESPONSES]
ADD CONSTRAINT [CK_AGENT_RESPONSES_accepted_prediction_token_count]
    CHECK ([accepted_prediction_token_count] IS NULL OR [accepted_prediction_token_count] >= 0);
GO

ALTER TABLE [dbo].[AGENT_RESPONSES]
ADD CONSTRAINT [CK_AGENT_RESPONSES_rejected_prediction_token_count]
    CHECK ([rejected_prediction_token_count] IS NULL OR [rejected_prediction_token_count] >= 0);
GO

ALTER TABLE [dbo].[AGENT_RESPONSES]
ADD CONSTRAINT [CK_AGENT_RESPONSES_input_audio_token_count]
    CHECK ([input_audio_token_count] IS NULL OR [input_audio_token_count] >= 0);
GO

ALTER TABLE [dbo].[AGENT_RESPONSES]
ADD CONSTRAINT [CK_AGENT_RESPONSES_cached_input_token_count]
    CHECK ([cached_input_token_count] IS NULL OR [cached_input_token_count] >= 0);
GO
