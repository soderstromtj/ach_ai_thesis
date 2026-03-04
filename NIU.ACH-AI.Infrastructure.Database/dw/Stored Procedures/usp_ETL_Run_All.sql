
-- ======================================================================
-- MASTER ORCHESTRATION PROCEDURE
-- ======================================================================

CREATE   PROCEDURE [dw].[usp_ETL_Run_All]
AS
BEGIN
    BEGIN TRY
        BEGIN TRANSACTION;
        -- 1. Load Dimensions
        EXEC [dw].[usp_ETL_Load_DimExperiment];
        EXEC [dw].[usp_ETL_Load_DimACHStep];
        EXEC [dw].[usp_ETL_Load_DimAgent];
        EXEC [dw].[usp_ETL_Load_DimEvidence];
        EXEC [dw].[usp_ETL_Load_DimHypothesis];
        EXEC [dw].[usp_ETL_Load_DimEvaluationScore];
        
        -- 2. Load Facts (Order matters)
        EXEC [dw].[usp_ETL_Load_FactStepExecution];
        EXEC [dw].[usp_ETL_Load_FactAgentResponse];
        EXEC [dw].[usp_ETL_Load_FactEvaluation];
        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END;