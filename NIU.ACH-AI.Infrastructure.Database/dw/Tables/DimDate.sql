CREATE TABLE [dw].[DimDate] (
    [DateKey]       INT           NOT NULL,
    [Date]          DATE          NOT NULL,
    [Year]          INT           NOT NULL,
    [Month]         INT           NOT NULL,
    [MonthName]     NVARCHAR (20) NOT NULL,
    [Day]           INT           NOT NULL,
    [DayOfWeekName] NVARCHAR (20) NOT NULL,
    PRIMARY KEY CLUSTERED ([DateKey] ASC)
);

