CREATE TABLE [dw].[FactAgentResponse] (
    [AgentResponseSK]     INT              IDENTITY (1, 1) NOT NULL,
    [AgentResponseBK]     UNIQUEIDENTIFIER NOT NULL,
    [StepExecutionSK]     INT              NOT NULL,
    [AgentSK]             INT              NOT NULL,
    [CreatedAt]           DATETIME2 (7)    NOT NULL,
    [FinishedAt]          DATETIME2 (7)    NOT NULL,
    [TurnNumber]          INT              NULL,
    [InputTokenCount]     INT              NULL,
    [OutputTokenCount]    INT              NULL,
    [ReasoningTokenCount] INT              NULL,
    [TotalTokens]         INT              NULL,
    [EstimatedCostUSD]    DECIMAL (18, 6)  NULL,
    [Content]             NVARCHAR (MAX)   NULL,
    PRIMARY KEY CLUSTERED ([AgentResponseSK] ASC),
    CONSTRAINT [FK_FactAgentResponse_Agent] FOREIGN KEY ([AgentSK]) REFERENCES [dw].[DimAgent] ([AgentSK]),
    CONSTRAINT [FK_FactAgentResponse_StepExecution] FOREIGN KEY ([StepExecutionSK]) REFERENCES [dw].[FactStepExecution] ([StepExecutionSK])
);

