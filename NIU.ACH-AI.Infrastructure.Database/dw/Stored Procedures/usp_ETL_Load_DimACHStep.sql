
CREATE   PROCEDURE [dw].[usp_ETL_Load_DimACHStep]
AS
BEGIN
    SET NOCOUNT ON;
    MERGE INTO [dw].[DimACHStep] AS Target
    USING (
        SELECT ach_step_id, step_name, primary_ach_step, step_order, description 
        FROM [dbo].[ACH_STEPS]
    ) AS Source ON Target.ACHStepBK = Source.ach_step_id
    WHEN MATCHED THEN UPDATE SET 
        Target.StepName = Source.step_name,
        Target.PrimaryACHStep = Source.primary_ach_step,
        Target.StepOrder = Source.step_order
        -- Note: StepDescription removed to match your updated DDL from the previous prompt
    WHEN NOT MATCHED BY TARGET THEN INSERT (ACHStepBK, StepName, PrimaryACHStep, StepOrder)
    VALUES (Source.ach_step_id, Source.step_name, Source.primary_ach_step, Source.step_order);
END;