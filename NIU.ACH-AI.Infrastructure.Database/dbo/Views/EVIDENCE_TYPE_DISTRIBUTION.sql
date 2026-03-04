CREATE VIEW [dbo].[EVIDENCE_TYPE_DISTRIBUTION]
	AS 
SELECT 
    e.experiment_id,
    e.experiment_name,
    et.evidence_type_name AS EvidenceType,
    COUNT(ev.evidence_id) AS EvidenceCount,
    -- Calculate the percentage of this evidence type out of the total evidence for the experiment
    CAST(COUNT(ev.evidence_id) AS FLOAT) / SUM(COUNT(ev.evidence_id)) OVER(PARTITION BY e.experiment_id) * 100 AS PercentageOfTotal
FROM 
    [dbo].[EXPERIMENTS] e
INNER JOIN 
    [dbo].[STEP_EXECUTIONS] se ON e.experiment_id = se.experiment_id
INNER JOIN 
    [dbo].[EVIDENCE] ev ON se.step_execution_id = ev.step_execution_id
INNER JOIN 
    [dbo].[EVIDENCE_TYPES] et ON ev.evidence_type_id = et.evidence_type_id
GROUP BY 
    e.experiment_id,
    e.experiment_name,
    et.evidence_type_name
