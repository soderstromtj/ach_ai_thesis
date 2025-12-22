-- PROVIDERS: AI service providers (OpenAI, Anthropic, Google, etc.)
CREATE TABLE [dbo].[PROVIDERS]
(
	[provider_id] UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
    [provider_name] NVARCHAR(50) NOT NULL,      -- e.g., "OpenAI", "Anthropic"
    [description] NVARCHAR(255) NULL,           -- Additional provider information
    [is_active] BIT NOT NULL DEFAULT 1,         -- Whether this provider is currently enabled
    [created_at] DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
)
GO

-- Index for filtering active providers
CREATE NONCLUSTERED INDEX [IX_PROVIDERS_is_active]
    ON [dbo].[PROVIDERS]([is_active])
    INCLUDE ([provider_name]);
GO

-- Index for provider name lookups
CREATE NONCLUSTERED INDEX [IX_PROVIDERS_provider_name]
    ON [dbo].[PROVIDERS]([provider_name]);
GO
