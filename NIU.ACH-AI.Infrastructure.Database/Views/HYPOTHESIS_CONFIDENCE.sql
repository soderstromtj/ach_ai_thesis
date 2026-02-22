CREATE VIEW [dbo].[HYPOTHESIS_CONFIDENCE]
	AS
SELECT 
	h.hypothesis_id,
    h.short_title AS Hypothesis_Short,
	h.hypothesis_text AS Hypothesis_Long,
    COUNT(ehe.evidence_hypothesis_evaluation_id) AS TotalEvaluations,
    AVG(ehe.confidence_score) AS AverageConfidence,
    MIN(ehe.confidence_score) AS LowestConfidence,
    MAX(ehe.confidence_score) AS HighestConfidence,
    -- Calculate how many evaluations were below a certain threshold (e.g., 0.5 or 50% confidence)
    SUM(CASE WHEN ehe.confidence_score < 0.5 THEN 1 ELSE 0 END) AS LowConfidenceEvaluationsCount
FROM 
    [dbo].[HYPOTHESES] h
INNER JOIN 
    [dbo].[EVIDENCE_HYPOTHESIS_EVALUATIONS] ehe ON h.hypothesis_id = ehe.hypothesis_id
GROUP BY 
    h.hypothesis_id, 
    h.short_title,
	h.hypothesis_text
