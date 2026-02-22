CREATE VIEW [dbo].[PARADOXICAL_EVIDENCE]
	AS
SELECT 
	ev.evidence_id,
    ev.claim AS EvidenceClaim,
    ISNULL(STDEVP(s.score_value), 0) AS DiagnosticityMetric,
    AVG(ehe.confidence_score) AS AverageConfidenceScore,
    COUNT(ehe.hypothesis_id) AS HypothesesEvaluated
FROM 
    [dbo].[EVIDENCE] ev
INNER JOIN 
    [dbo].[EVIDENCE_HYPOTHESIS_EVALUATIONS] ehe ON ev.evidence_id = ehe.evidence_id
INNER JOIN 
    [dbo].[EVALUATION_SCORES] s ON ehe.evaluation_score_id = s.evaluation_score_id
GROUP BY 
    ev.evidence_id, 
    ev.claim
HAVING 
    COUNT(ehe.hypothesis_id) > 1 
    -- Filter for evidence that is highly diagnostic but has low average confidence
    AND ISNULL(STDEVP(s.score_value), 0) >= 1.0 
    AND AVG(ehe.confidence_score) < 0.7 
