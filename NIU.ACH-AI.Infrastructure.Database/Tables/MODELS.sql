CREATE TABLE [dbo].[MODELS]
(
	[model_id] UNIQUEIDENTIFIER NOT NULL PRIMARY KEY, 
    [provider_id] UNIQUEIDENTIFIER NOT NULL,
    [model_name] NVARCHAR(50) NOT NULL, 
    [input_token_cost] DECIMAL(6, 2) NULL, 
    [cached_input_token_cost] DECIMAL(6, 2) NULL, 
    [output_token_cost] DECIMAL(6, 2) NULL, 
    CONSTRAINT [FK_MODELS_PROVIDERS] FOREIGN KEY ([provider_id]) REFERENCES [PROVIDERS]([provider_id])
)
