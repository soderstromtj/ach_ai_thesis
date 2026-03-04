CREATE   PROCEDURE [dw].[usp_DW_Wipe_All_Data]
AS
BEGIN
    SET NOCOUNT ON;

    BEGIN TRY
        BEGIN TRANSACTION;

        -- 1. Wipe the Fact Tables first (The children)
        DELETE FROM [dw].[FactEvaluation];
        DELETE FROM [dw].[FactAgentResponse];
        DELETE FROM [dw].[FactStepExecution];

        -- 2. Wipe the Dimension Tables next (The parents)
        DELETE FROM [dw].[DimEvaluationScore];
        DELETE FROM [dw].[DimHypothesis];
        DELETE FROM [dw].[DimEvidence];
        DELETE FROM [dw].[DimAgent];
        DELETE FROM [dw].[DimACHStep];
        DELETE FROM [dw].[DimExperiment];
        
        -- Note: [dw].[DimDate] is intentionally skipped to preserve your calendar.

        -- 3. Reseed the Identity Columns back to 0 (so the next insert becomes SK 1)
        DBCC CHECKIDENT ('[dw].[FactEvaluation]', RESEED, 0);
        DBCC CHECKIDENT ('[dw].[FactAgentResponse]', RESEED, 0);
        DBCC CHECKIDENT ('[dw].[FactStepExecution]', RESEED, 0);
        
        DBCC CHECKIDENT ('[dw].[DimEvaluationScore]', RESEED, 0);
        DBCC CHECKIDENT ('[dw].[DimHypothesis]', RESEED, 0);
        DBCC CHECKIDENT ('[dw].[DimEvidence]', RESEED, 0);
        DBCC CHECKIDENT ('[dw].[DimAgent]', RESEED, 0);
        DBCC CHECKIDENT ('[dw].[DimACHStep]', RESEED, 0);
        DBCC CHECKIDENT ('[dw].[DimExperiment]', RESEED, 0);

        COMMIT TRANSACTION;
        PRINT 'Data Warehouse tables successfully wiped and Surrogate Keys reseeded.';
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;
            
        -- Surface the exact error
        DECLARE @ErrorMessage NVARCHAR(4000) = ERROR_MESSAGE();
        DECLARE @ErrorSeverity INT = ERROR_SEVERITY();
        DECLARE @ErrorState INT = ERROR_STATE();
        
        RAISERROR (@ErrorMessage, @ErrorSeverity, @ErrorState);
    END CATCH
END;