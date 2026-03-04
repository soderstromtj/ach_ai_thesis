
-- ======================================================================
-- DIMENSION ETL PROCEDURES (Source: dbo -> Target: dw)
-- ======================================================================

CREATE   PROCEDURE [dw].[usp_ETL_Load_DimExperiment]
AS
BEGIN
    SET NOCOUNT ON;
    MERGE INTO [dw].[DimExperiment] AS Target
    USING (
        SELECT e.experiment_id, e.experiment_name, e.key_question, s.context 
        FROM [dbo].[EXPERIMENTS] e
        INNER JOIN [dbo].[SCENARIOS] s ON e.scenario_id = s.scenario_id
    ) AS Source ON Target.ExperimentBK = Source.experiment_id
    WHEN MATCHED THEN UPDATE SET 
        Target.ExperimentName = Source.experiment_name,
        Target.KeyQuestion = Source.key_question,
        Target.ScenarioContext = Source.context
    WHEN NOT MATCHED BY TARGET THEN INSERT (ExperimentBK, ExperimentName, KeyQuestion, ScenarioContext)
    VALUES (Source.experiment_id, Source.experiment_name, Source.key_question, Source.context);
END;