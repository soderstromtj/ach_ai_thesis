CREATE   PROCEDURE [dw].[usp_ETL_Load_FactAgentResponse]
AS
BEGIN
    SET NOCOUNT ON;
    
    INSERT INTO [dw].[FactAgentResponse] (
        AgentResponseBK, StepExecutionSK, AgentSK, 
        CreatedAt, FinishedAt, 
        TurnNumber, InputTokenCount, OutputTokenCount, ReasoningTokenCount, TotalTokens, EstimatedCostUSD
    )
    SELECT 
        ar.agent_response_id,
        ISNULL(fse.StepExecutionSK, -1),
        ISNULL(da.AgentSK, -1),
        ar.created_at,   
        ar.finished_at,  
        ar.turn_number,
        ar.input_token_count,
        ar.output_token_count,
        ar.reasoning_token_count,
        (ISNULL(ar.input_token_count, 0) + ISNULL(ar.output_token_count, 0)),
        
        -- Corrected Cost Calculation: (Tokens * PricePerMillion) / 1,000,000
        CAST(
            (
                (ISNULL(ar.input_token_count, 0) * ISNULL(m.input_token_cost, 0)) + 
                (ISNULL(ar.output_token_count, 0) * ISNULL(m.output_token_cost, 0))
            ) / 1000000.0 
        AS DECIMAL(18, 6)) AS EstimatedCostUSD

    FROM [dbo].[AGENT_RESPONSES] ar
    LEFT JOIN [dw].[FactStepExecution] fse ON ar.step_execution_id = fse.StepExecutionBK
    LEFT JOIN [dw].[DimAgent] da ON ar.agent_configuration_id = da.AgentConfigurationBK
    LEFT JOIN [dbo].[AGENT_CONFIGURATIONS] ac ON ar.agent_configuration_id = ac.agent_configuration_id
    LEFT JOIN [dbo].[MODELS] m ON ac.model_id = m.model_id
    WHERE NOT EXISTS (SELECT 1 FROM [dw].[FactAgentResponse] f WHERE f.AgentResponseBK = ar.agent_response_id);
END;