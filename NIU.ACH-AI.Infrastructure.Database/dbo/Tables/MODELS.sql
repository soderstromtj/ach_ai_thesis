-- Models define AI models and their pricing information
-- Used to calculate costs for agent responses based on token usage
CREATE TABLE [dbo].[MODELS] (
    [model_id]                UNIQUEIDENTIFIER NOT NULL,
    [provider_id]             UNIQUEIDENTIFIER NOT NULL,
    [model_name]              NVARCHAR (50)    NOT NULL,
    [input_token_cost]        DECIMAL (12, 8)  NULL,
    [cached_input_token_cost] DECIMAL (12, 8)  NULL,
    [output_token_cost]       DECIMAL (12, 8)  NULL,
    PRIMARY KEY CLUSTERED ([model_id] ASC),
    CONSTRAINT [FK_MODELS_PROVIDERS] FOREIGN KEY ([provider_id]) REFERENCES [dbo].[PROVIDERS] ([provider_id]),
    CONSTRAINT [UQ_MODELS_provider_model] UNIQUE NONCLUSTERED ([provider_id] ASC, [model_name] ASC)
);
GO

-- Create the index for faster lookups by provider
CREATE NONCLUSTERED INDEX [IX_MODELS_provider_id]
ON [dbo].[MODELS] ([provider_id])
INCLUDE ([input_token_cost], [cached_input_token_cost], [output_token_cost]);
GO
CREATE NONCLUSTERED INDEX [IX_MODELS_model_name]
    ON [dbo].[MODELS]([model_name] ASC)
    INCLUDE([provider_id]);

