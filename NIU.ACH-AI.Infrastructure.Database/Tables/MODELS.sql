-- Models define AI models and their pricing information
-- Used to calculate costs for agent responses based on token usage
CREATE TABLE [dbo].[MODELS]
(
	[model_id] UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
    [provider_id] UNIQUEIDENTIFIER NOT NULL,        -- AI service provider (OpenAI, Anthropic, etc.)
    [model_name] NVARCHAR(50) NOT NULL,             -- Model identifier (gpt-4, claude-3-opus, etc.)
    [input_token_cost] DECIMAL(10, 8) NULL,         -- Cost per 1M input token in USD
    [cached_input_token_cost] DECIMAL(10, 8) NULL,  -- Cost per 1M cached input token in USD
    [output_token_cost] DECIMAL(10, 8) NULL,        -- Cost per 1M output token in USD

    -- Foreign key constraints
    CONSTRAINT [FK_MODELS_PROVIDERS]
        FOREIGN KEY ([provider_id]) REFERENCES [PROVIDERS]([provider_id]),

    -- Unique constraint to prevent duplicate model names per provider
    CONSTRAINT [UQ_MODELS_provider_model]
        UNIQUE ([provider_id], [model_name])
)
GO

-- Index for finding all models by provider
CREATE NONCLUSTERED INDEX [IX_MODELS_provider_id]
    ON [dbo].[MODELS]([provider_id])
    INCLUDE ([model_name], [input_token_cost], [output_token_cost]);
GO

-- Index for model name lookups
CREATE NONCLUSTERED INDEX [IX_MODELS_model_name]
    ON [dbo].[MODELS]([model_name])
    INCLUDE ([provider_id]);
GO
