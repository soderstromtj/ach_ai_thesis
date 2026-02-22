CREATE VIEW [dbo].[AGENT_COSTS]
	AS 
SELECT
    ar.agent_name,
    SUM(
        -- Calculate cost for standard input tokens
        (COALESCE(ar.input_token_count, 0) * COALESCE(m.input_token_cost, 0) / 1000000.0) +
        
        -- Calculate cost for cached input tokens
        (COALESCE(ar.cached_input_token_count, 0) * COALESCE(m.cached_input_token_cost, 0) / 1000000.0) +
        
        -- Calculate cost for output tokens
        (COALESCE(ar.output_token_count, 0) * COALESCE(m.output_token_cost, 0) / 1000000.0)
    ) AS TotalCost
FROM
    [dbo].[AGENT_RESPONSES] ar
INNER JOIN
    [dbo].[AGENT_CONFIGURATIONS] ac ON ar.agent_configuration_id = ac.agent_configuration_id
INNER JOIN
    [dbo].[MODELS] m ON ac.model_id = m.model_id
GROUP BY
    ar.agent_name
