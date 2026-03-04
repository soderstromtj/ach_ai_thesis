
CREATE   PROCEDURE [dw].[usp_ETL_Load_DimHypothesis]
AS
BEGIN
    SET NOCOUNT ON;
    MERGE INTO [dw].[DimHypothesis] AS Target
    USING (
        SELECT hypothesis_id, short_title, hypothesis_text, is_refined 
        FROM [dbo].[HYPOTHESES]
    ) AS Source ON Target.HypothesisBK = Source.hypothesis_id
    WHEN MATCHED THEN UPDATE SET 
        Target.ShortTitle = Source.short_title, Target.HypothesisText = Source.hypothesis_text, Target.IsRefined = Source.is_refined
    WHEN NOT MATCHED BY TARGET THEN INSERT (HypothesisBK, ShortTitle, HypothesisText, IsRefined)
    VALUES (Source.hypothesis_id, Source.short_title, Source.hypothesis_text, Source.is_refined);
END;