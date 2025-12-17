CREATE TABLE [dbo].[EVALUATION_SCORES]
(
	[evaluation_score_id] INT NOT NULL PRIMARY KEY IDENTITY(1,1), 
    [score_name] NVARCHAR(50) NOT NULL, 
    [score_value] INT NOT NULL, 
    [description] NVARCHAR(255) NULL
)
