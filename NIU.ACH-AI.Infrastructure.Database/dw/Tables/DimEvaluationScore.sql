CREATE TABLE [dw].[DimEvaluationScore] (
    [EvaluationScoreSK] INT           IDENTITY (1, 1) NOT NULL,
    [EvaluationScoreBK] INT           NOT NULL,
    [ScoreName]         NVARCHAR (50) NOT NULL,
    [ScoreValue]        INT           NOT NULL,
    PRIMARY KEY CLUSTERED ([EvaluationScoreSK] ASC)
);

