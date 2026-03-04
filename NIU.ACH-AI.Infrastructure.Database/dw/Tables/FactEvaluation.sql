CREATE TABLE [dw].[FactEvaluation] (
    [EvaluationSK]        INT              IDENTITY (1, 1) NOT NULL,
    [EvaluationBK]        UNIQUEIDENTIFIER NOT NULL,
    [StepExecutionSK]     INT              NOT NULL,
    [HypothesisSK]        INT              NOT NULL,
    [EvidenceSK]          INT              NOT NULL,
    [EvaluationScoreSK]   INT              NOT NULL,
    [CreatedAt]           DATETIME2 (7)    NOT NULL,
    [ConfidenceScore]     DECIMAL (5, 4)   NULL,
    [Rationale]           NVARCHAR (MAX)   NULL,
    [ConfidenceRationale] NVARCHAR (MAX)   NULL,
    PRIMARY KEY CLUSTERED ([EvaluationSK] ASC),
    CONSTRAINT [FK_FactEvaluation_Evidence] FOREIGN KEY ([EvidenceSK]) REFERENCES [dw].[DimEvidence] ([EvidenceSK]),
    CONSTRAINT [FK_FactEvaluation_Hypothesis] FOREIGN KEY ([HypothesisSK]) REFERENCES [dw].[DimHypothesis] ([HypothesisSK]),
    CONSTRAINT [FK_FactEvaluation_Score] FOREIGN KEY ([EvaluationScoreSK]) REFERENCES [dw].[DimEvaluationScore] ([EvaluationScoreSK]),
    CONSTRAINT [FK_FactEvaluation_StepExecution] FOREIGN KEY ([StepExecutionSK]) REFERENCES [dw].[FactStepExecution] ([StepExecutionSK])
);

