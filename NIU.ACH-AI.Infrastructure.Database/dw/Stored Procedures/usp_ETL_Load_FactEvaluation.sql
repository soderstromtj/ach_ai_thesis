
CREATE   PROCEDURE [dw].[usp_ETL_Load_FactEvaluation]
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO [dw].[FactEvaluation] (EvaluationBK, StepExecutionSK, HypothesisSK, EvidenceSK, EvaluationScoreSK, CreatedAt, ConfidenceScore, Rationale, ConfidenceRationale)
    SELECT 
        ehe.evidence_hypothesis_evaluation_id,
        ISNULL(fse.StepExecutionSK, -1),
        ISNULL(dh.HypothesisSK, -1),
        ISNULL(de.EvidenceSK, -1),
        ISNULL(des.EvaluationScoreSK, -1),
        ehe.created_at, -- Raw timestamp
        ehe.confidence_score,
        ehe.rationale,
        ehe.confidence_rationale
    FROM [dbo].[EVIDENCE_HYPOTHESIS_EVALUATIONS] ehe
    LEFT JOIN [dw].[FactStepExecution] fse ON ehe.step_execution_id = fse.StepExecutionBK
    LEFT JOIN [dw].[DimHypothesis] dh ON ehe.hypothesis_id = dh.HypothesisBK
    LEFT JOIN [dw].[DimEvidence] de ON ehe.evidence_id = de.EvidenceBK
    LEFT JOIN [dw].[DimEvaluationScore] des ON ehe.evaluation_score_id = des.EvaluationScoreBK
    WHERE NOT EXISTS (SELECT 1 FROM [dw].[FactEvaluation] f WHERE f.EvaluationBK = ehe.evidence_hypothesis_evaluation_id);
END;