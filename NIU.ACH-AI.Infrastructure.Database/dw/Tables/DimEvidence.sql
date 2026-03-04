CREATE TABLE [dw].[DimEvidence] (
    [EvidenceSK]          INT              IDENTITY (1, 1) NOT NULL,
    [EvidenceBK]          UNIQUEIDENTIFIER NOT NULL,
    [EvidenceTypeName]    NVARCHAR (50)    NOT NULL,
    [Claim]               NVARCHAR (MAX)   NOT NULL,
    [ReferenceSnippet]    NVARCHAR (MAX)   NULL,
    [HypothesesEvaluated] INT              NULL,
    [DiagnosticityMetric] DECIMAL (18, 4)  NULL,
    [LowestScore]         INT              NULL,
    [HighestScore]        INT              NULL,
    PRIMARY KEY CLUSTERED ([EvidenceSK] ASC)
);

