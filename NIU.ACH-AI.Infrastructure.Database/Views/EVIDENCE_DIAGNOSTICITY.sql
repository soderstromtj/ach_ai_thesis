CREATE VIEW [dbo].[EVIDENCE_DIAGNOSTICITY]
	AS 
SELECT 
    e.evidence_id,
    e.claim AS EvidenceClaim,
    -- Count how many hypotheses this evidence was evaluated against
    COUNT(ehe.hypothesis_id) AS HypothesesEvaluated,
    
    -- Calculate the Standard Deviation of the scores to represent Diagnosticity
    -- Note: STDEVP() is population standard deviation. STDEV() is sample standard deviation.
    -- We use STDEVP() here because we are evaluating it against the full population of our generated hypotheses.
    ISNULL(STDEVP(s.score_value), 0) AS DiagnosticityMetric,
    
    -- Also showing Min and Max scores to give context to the spread
    MIN(s.score_value) AS LowestScore,
    MAX(s.score_value) AS HighestScore
FROM 
    [dbo].[EVIDENCE] e
INNER JOIN 
    [dbo].[EVIDENCE_HYPOTHESIS_EVALUATIONS] ehe ON e.evidence_id = ehe.evidence_id
INNER JOIN 
    [dbo].[EVALUATION_SCORES] s ON ehe.evaluation_score_id = s.evaluation_score_id
GROUP BY 
    e.evidence_id, 
    e.claim
HAVING 
    -- Filtering out evidence that hasn't been evaluated against more than 1 hypothesis,
    -- since variance/diagnosticity requires at least two data points to be meaningful.
    COUNT(ehe.hypothesis_id) > 1
