CREATE   PROCEDURE [dw].[usp_ETL_Load_DimEvidence]
AS
BEGIN
    SET NOCOUNT ON;

    -- 1. CTE to calculate the diagnosticity metrics at the evidence grain
    WITH EvidenceDiagnosticity AS (
        SELECT 
            e.evidence_id,
            COUNT(ehe.hypothesis_id) AS HypothesesEvaluated,
            ISNULL(STDEVP(CAST(s.score_value AS FLOAT)), 0) AS DiagnosticityMetric,
            MIN(s.score_value) AS LowestScore,
            MAX(s.score_value) AS HighestScore
        FROM [dbo].[EVIDENCE] e
        LEFT JOIN [dbo].[EVIDENCE_HYPOTHESIS_EVALUATIONS] ehe ON e.evidence_id = ehe.evidence_id
        LEFT JOIN [dbo].[EVALUATION_SCORES] s ON ehe.evaluation_score_id = s.evaluation_score_id
        GROUP BY e.evidence_id
    )
    
    -- 2. MERGE the text attributes and the calculated metrics into the dimension
    MERGE INTO [dw].[DimEvidence] AS Target
    USING (
        SELECT 
            e.evidence_id, 
            ISNULL(et.evidence_type_name, 'Unknown') AS evidence_type_name, 
            e.claim, 
            e.reference_snippet,
            ed.HypothesesEvaluated,
            CAST(ed.DiagnosticityMetric AS DECIMAL(18,4)) AS DiagnosticityMetric,
            ed.LowestScore,
            ed.HighestScore
        FROM [dbo].[EVIDENCE] e
        LEFT JOIN [dbo].[EVIDENCE_TYPES] et ON e.evidence_type_id = et.evidence_type_id
        LEFT JOIN EvidenceDiagnosticity ed ON e.evidence_id = ed.evidence_id
    ) AS Source ON Target.EvidenceBK = Source.evidence_id
    
    WHEN MATCHED THEN UPDATE SET 
        Target.EvidenceTypeName = Source.evidence_type_name, 
        Target.Claim = Source.claim, 
        Target.ReferenceSnippet = Source.reference_snippet,
        Target.HypothesesEvaluated = Source.HypothesesEvaluated,
        Target.DiagnosticityMetric = Source.DiagnosticityMetric,
        Target.LowestScore = Source.LowestScore,
        Target.HighestScore = Source.HighestScore
        
    WHEN NOT MATCHED BY TARGET THEN INSERT (
        EvidenceBK, EvidenceTypeName, Claim, ReferenceSnippet, 
        HypothesesEvaluated, DiagnosticityMetric, LowestScore, HighestScore
    )
    VALUES (
        Source.evidence_id, Source.evidence_type_name, Source.claim, Source.reference_snippet, 
        Source.HypothesesEvaluated, Source.DiagnosticityMetric, Source.LowestScore, Source.HighestScore
    );
END;