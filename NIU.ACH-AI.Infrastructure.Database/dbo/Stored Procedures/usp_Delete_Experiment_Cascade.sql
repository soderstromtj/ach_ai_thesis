CREATE   PROCEDURE [dbo].[usp_Delete_Experiment_Cascade]
    @ExperimentID UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;

    BEGIN TRY
        BEGIN TRANSACTION;

        -- 1. Get all Step Executions tied to this Experiment
        -- We store these in a temporary table variable to make the downstream deletes faster and cleaner
        DECLARE @StepExecutions TABLE (step_execution_id UNIQUEIDENTIFIER);
        
        INSERT INTO @StepExecutions (step_execution_id)
        SELECT step_execution_id 
        FROM [dbo].[STEP_EXECUTIONS] 
        WHERE experiment_id = @ExperimentID;

        -- 2. Delete the lowest level child records first (Evaluations and Agent Responses)
        DELETE FROM [dbo].[EVIDENCE_HYPOTHESIS_EVALUATIONS]
        WHERE step_execution_id IN (SELECT step_execution_id FROM @StepExecutions);

        DELETE FROM [dbo].[AGENT_RESPONSES]
        WHERE step_execution_id IN (SELECT step_execution_id FROM @StepExecutions);

        -- 3. Delete the mid-level configurations and outputs
        DELETE FROM [dbo].[AGENT_CONFIGURATIONS]
        WHERE step_execution_id IN (SELECT step_execution_id FROM @StepExecutions);

        DELETE FROM [dbo].[EVIDENCE]
        WHERE step_execution_id IN (SELECT step_execution_id FROM @StepExecutions);

        DELETE FROM [dbo].[HYPOTHESES]
        WHERE step_execution_id IN (SELECT step_execution_id FROM @StepExecutions);

        -- 4. Delete the Step Executions
        DELETE FROM [dbo].[STEP_EXECUTIONS]
        WHERE experiment_id = @ExperimentID;

        -- 5. Finally, delete the parent Experiment
        DELETE FROM [dbo].[EXPERIMENTS]
        WHERE experiment_id = @ExperimentID;

        -- Note: We are NOT deleting from SCENARIOS, as scenarios are typically 
        -- master reference data that might be shared across multiple experiments.

        COMMIT TRANSACTION;
        PRINT 'Successfully deleted Experiment and all cascaded dependencies.';
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;
            
        -- Surface the exact error to the user
        DECLARE @ErrorMessage NVARCHAR(4000) = ERROR_MESSAGE();
        DECLARE @ErrorSeverity INT = ERROR_SEVERITY();
        DECLARE @ErrorState INT = ERROR_STATE();
        
        RAISERROR (@ErrorMessage, @ErrorSeverity, @ErrorState);
    END CATCH
END;