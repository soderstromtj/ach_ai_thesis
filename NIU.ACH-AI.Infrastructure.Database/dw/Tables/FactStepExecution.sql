CREATE TABLE [dw].[FactStepExecution] (
    [StepExecutionSK] INT              IDENTITY (1, 1) NOT NULL,
    [StepExecutionBK] UNIQUEIDENTIFIER NOT NULL,
    [ExperimentSK]    INT              NOT NULL,
    [ACHStepSK]       INT              NOT NULL,
    [StartTime]       DATETIME2 (7)    NULL,
    [EndTime]         DATETIME2 (7)    NULL,
    [RetryCount]      INT              NOT NULL,
    [DurationSeconds] BIGINT           NULL,
    [ExecutionStatus] NVARCHAR (50)    NOT NULL,
    [HasError]        BIT              NOT NULL,
    PRIMARY KEY CLUSTERED ([StepExecutionSK] ASC),
    CONSTRAINT [FK_FactStepExecution_ACHStep] FOREIGN KEY ([ACHStepSK]) REFERENCES [dw].[DimACHStep] ([ACHStepSK]),
    CONSTRAINT [FK_FactStepExecution_Experiment] FOREIGN KEY ([ExperimentSK]) REFERENCES [dw].[DimExperiment] ([ExperimentSK])
);

