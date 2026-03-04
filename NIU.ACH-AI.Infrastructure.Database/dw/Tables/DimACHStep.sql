CREATE TABLE [dw].[DimACHStep] (
    [ACHStepSK]      INT            IDENTITY (1, 1) NOT NULL,
    [ACHStepBK]      INT            NOT NULL,
    [StepName]       NVARCHAR (100) NOT NULL,
    [PrimaryACHStep] INT            NOT NULL,
    [StepOrder]      INT            NOT NULL,
    PRIMARY KEY CLUSTERED ([ACHStepSK] ASC)
);

