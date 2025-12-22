-- EVALUATION_SCORES: Reference table for evidence-hypothesis evaluation scores
-- Examples: "Consistent" (+1), "Inconsistent" (-1), "Neutral" (0)
CREATE TABLE [dbo].[EVALUATION_SCORES]
(
	[evaluation_score_id] INT NOT NULL PRIMARY KEY IDENTITY(1,1),
    [score_name] NVARCHAR(50) NOT NULL,         -- e.g., "Consistent", "Inconsistent", "Neutral"
    [score_value] INT NOT NULL,                 -- Numeric value for scoring calculations
    [description] NVARCHAR(255) NULL            -- Explanation of what this score means
)
GO

-- Index for lookup by score name
CREATE NONCLUSTERED INDEX [IX_EVALUATION_SCORES_score_name]
    ON [dbo].[EVALUATION_SCORES]([score_name]);
GO
