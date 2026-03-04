-- ======================================================================
-- UPDATED FACT ETL PROCEDURES
-- ======================================================================

CREATE   PROCEDURE [dw].[usp_ETL_Load_FactStepExecution]
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO [dw].[FactStepExecution] (StepExecutionBK, ExperimentSK, ACHStepSK, StartTime, EndTime, RetryCount, DurationSeconds, ExecutionStatus, HasError)
    SELECT 
        se.step_execution_id,
        ISNULL(de.ExperimentSK, -1),
        ISNULL(da.ACHStepSK, -1),
        se.datetime_start, -- Raw timestamp
        se.datetime_end,   -- Raw timestamp
        ISNULL(se.retry_count, 0),
        DATEDIFF(SECOND, se.datetime_start, se.datetime_end),
        ISNULL(se.execution_status, 'Unknown'),
        CAST(CASE WHEN se.error_message IS NOT NULL THEN 1 ELSE 0 END AS BIT)
    FROM [dbo].[STEP_EXECUTIONS] se
    LEFT JOIN [dw].[DimExperiment] de ON se.experiment_id = de.ExperimentBK
    LEFT JOIN [dw].[DimACHStep] da ON se.ach_step_id = da.ACHStepBK
    WHERE NOT EXISTS (SELECT 1 FROM [dw].[FactStepExecution] f WHERE f.StepExecutionBK = se.step_execution_id);
END;