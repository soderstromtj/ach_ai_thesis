CREATE TABLE [dw].[DimExperiment] (
    [ExperimentSK]    INT              IDENTITY (1, 1) NOT NULL,
    [ExperimentBK]    UNIQUEIDENTIFIER NOT NULL,
    [ExperimentName]  NVARCHAR (50)    NOT NULL,
    [KeyQuestion]     NVARCHAR (255)   NOT NULL,
    [ScenarioContext] NVARCHAR (MAX)   NOT NULL,
    PRIMARY KEY CLUSTERED ([ExperimentSK] ASC)
);

