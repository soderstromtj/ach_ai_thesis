
CREATE   PROCEDURE [dw].[usp_ETL_Load_DimAgent]
AS
BEGIN
    SET NOCOUNT ON;
    MERGE INTO [dw].[DimAgent] AS Target
    USING (
        SELECT ac.agent_configuration_id, ac.agent_name, ISNULL(p.provider_name, 'Unknown') AS provider_name, ISNULL(m.model_name, 'Unknown') AS model_name, ac.instructions
        FROM [dbo].[AGENT_CONFIGURATIONS] ac
        LEFT JOIN [dbo].[PROVIDERS] p ON ac.provider_id = p.provider_id
        LEFT JOIN [dbo].[MODELS] m ON ac.model_id = m.model_id
    ) AS Source ON Target.AgentConfigurationBK = Source.agent_configuration_id
    WHEN MATCHED THEN UPDATE SET 
        Target.AgentName = Source.agent_name, Target.ProviderName = Source.provider_name, Target.ModelName = Source.model_name, Target.Instructions = Source.instructions
    WHEN NOT MATCHED BY TARGET THEN INSERT (AgentConfigurationBK, AgentName, ProviderName, ModelName, Instructions)
    VALUES (Source.agent_configuration_id, Source.agent_name, Source.provider_name, Source.model_name, Source.instructions);
END;