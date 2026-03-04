CREATE TABLE [dw].[DimHypothesis] (
    [HypothesisSK]   INT              IDENTITY (1, 1) NOT NULL,
    [HypothesisBK]   UNIQUEIDENTIFIER NOT NULL,
    [ShortTitle]     NVARCHAR (MAX)   NOT NULL,
    [HypothesisText] NVARCHAR (MAX)   NOT NULL,
    [IsRefined]      BIT              NOT NULL,
    PRIMARY KEY CLUSTERED ([HypothesisSK] ASC)
);

