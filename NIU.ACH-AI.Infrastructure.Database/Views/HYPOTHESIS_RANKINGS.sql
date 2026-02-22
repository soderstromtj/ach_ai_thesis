CREATE VIEW [dbo].[HYPOTHESIS_RANKINGS]
	AS
SELECT
	h.hypothesis_id AS Hypothesis_ID,
	h.short_title AS Hypothesis_ShortTitle,
    h.hypothesis_text AS Hypothesis,
    SUM(s.score_value) AS TotalScore,
    COUNT(CASE WHEN s.score_value < 0 THEN 1 END) AS InconsistentEvidenceCount,
    COUNT(CASE WHEN s.score_value > 0 THEN 1 END) AS ConsistentEvidenceCount,
    COUNT(CASE WHEN s.score_value = 0 THEN 1 END) AS NeutralEvidenceCount
FROM
    [dbo].[HYPOTHESES] h
LEFT JOIN
    [dbo].[EVIDENCE_HYPOTHESIS_EVALUATIONS] ehe ON h.hypothesis_id = ehe.hypothesis_id
LEFT JOIN
    [dbo].[EVALUATION_SCORES] s ON ehe.evaluation_score_id = s.evaluation_score_id
GROUP BY
    h.hypothesis_id, h.short_title, h.hypothesis_text
