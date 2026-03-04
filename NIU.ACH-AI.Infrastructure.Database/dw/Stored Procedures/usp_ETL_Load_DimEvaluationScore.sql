
CREATE   PROCEDURE [dw].[usp_ETL_Load_DimEvaluationScore]
AS
BEGIN
    SET NOCOUNT ON;
    MERGE INTO [dw].[DimEvaluationScore] AS Target
    USING (
        SELECT evaluation_score_id, score_name, score_value 
        FROM [dbo].[EVALUATION_SCORES]
    ) AS Source ON Target.EvaluationScoreBK = Source.evaluation_score_id
    WHEN MATCHED THEN UPDATE SET 
        Target.ScoreName = Source.score_name, Target.ScoreValue = Source.score_value
    WHEN NOT MATCHED BY TARGET THEN INSERT (EvaluationScoreBK, ScoreName, ScoreValue)
    VALUES (Source.evaluation_score_id, Source.score_name, Source.score_value);
END;