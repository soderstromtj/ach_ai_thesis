CREATE VIEW [dbo].[AUDIT_TRAIL_COMPLETENESS_RATE]
	AS
SELECT 
    COUNT(ehe.evidence_hypothesis_evaluation_id) AS TotalEvaluations,
    -- What percentage of evaluations have a written rationale?
    CAST(SUM(CASE WHEN ehe.rationale IS NOT NULL AND LEN(LTRIM(RTRIM(ehe.rationale))) > 10 THEN 1 ELSE 0 END) AS FLOAT) 
        / COUNT(*) * 100 AS PercentWithRationale,
    -- What percentage have a numeric confidence score?
    CAST(SUM(CASE WHEN ehe.confidence_score IS NOT NULL THEN 1 ELSE 0 END) AS FLOAT) 
        / COUNT(*) * 100 AS PercentWithConfidenceScore,
    -- What percentage have a written rationale explaining that confidence?
    CAST(SUM(CASE WHEN ehe.confidence_rationale IS NOT NULL AND LEN(LTRIM(RTRIM(ehe.confidence_rationale))) > 10 THEN 1 ELSE 0 END) AS FLOAT) 
        / COUNT(*) * 100 AS PercentWithConfidenceRationale
FROM 
    [dbo].[EVIDENCE_HYPOTHESIS_EVALUATIONS] ehe;
