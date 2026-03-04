CREATE TABLE [dw].[DimAgent] (
    [AgentSK]              INT              IDENTITY (1, 1) NOT NULL,
    [AgentConfigurationBK] UNIQUEIDENTIFIER NOT NULL,
    [AgentName]            NVARCHAR (50)    NOT NULL,
    [ProviderName]         NVARCHAR (50)    NOT NULL,
    [ModelName]            NVARCHAR (50)    NOT NULL,
    [Instructions]         NVARCHAR (MAX)   NOT NULL,
    PRIMARY KEY CLUSTERED ([AgentSK] ASC)
);

