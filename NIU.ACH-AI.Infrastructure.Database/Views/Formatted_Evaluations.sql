CREATE VIEW dbo.FORMATTED_EVALUATIONS
AS
SELECT         
    dbo.EXPERIMENTS.experiment_id AS ExperimentId, 

    -- New: Creates H1, H2, H3... restarting at 1 for each new Experiment
    CONCAT('H', DENSE_RANK() OVER(PARTITION BY dbo.EXPERIMENTS.experiment_id ORDER BY dbo.HYPOTHESES.hypothesis_id)) AS Hypothesis_Label,
    
    dbo.HYPOTHESES.short_title AS Hypothesis_Short, 
    dbo.HYPOTHESES.hypothesis_text AS Hypothesis_Long, 

    -- New: Creates E1, E2, E3... restarting at 1 for each new Experiment
    CONCAT('E', DENSE_RANK() OVER(PARTITION BY dbo.EXPERIMENTS.experiment_id ORDER BY dbo.EVIDENCE.evidence_id)) AS Evidence_Label,
    
    dbo.EVIDENCE.claim AS Evidence_Claim, 
    dbo.EVIDENCE.evidence_type_id AS Evidence_Type, 
    dbo.EVALUATION_SCORES.score_name AS Score_Name, 
    dbo.EVALUATION_SCORES.score_value AS Score_Value, 
    dbo.EVIDENCE_HYPOTHESIS_EVALUATIONS.rationale AS Score_Rationale, 
    dbo.EVIDENCE_HYPOTHESIS_EVALUATIONS.confidence_score AS Confidence_Score, 
    dbo.EVIDENCE_HYPOTHESIS_EVALUATIONS.confidence_rationale AS Confidence_Score_Rationale, 
    dbo.EVIDENCE.reference_snippet

FROM dbo.EVIDENCE_HYPOTHESIS_EVALUATIONS 
INNER JOIN dbo.EVIDENCE 
    ON dbo.EVIDENCE_HYPOTHESIS_EVALUATIONS.evidence_id = dbo.EVIDENCE.evidence_id 
INNER JOIN dbo.STEP_EXECUTIONS 
    ON dbo.EVIDENCE_HYPOTHESIS_EVALUATIONS.step_execution_id = dbo.STEP_EXECUTIONS.step_execution_id 
INNER JOIN dbo.EXPERIMENTS 
    ON dbo.STEP_EXECUTIONS.experiment_id = dbo.EXPERIMENTS.experiment_id 
INNER JOIN dbo.EVALUATION_SCORES 
    ON dbo.EVIDENCE_HYPOTHESIS_EVALUATIONS.evaluation_score_id = dbo.EVALUATION_SCORES.evaluation_score_id 
INNER JOIN dbo.HYPOTHESES 
    ON dbo.EVIDENCE_HYPOTHESIS_EVALUATIONS.hypothesis_id = dbo.HYPOTHESES.hypothesis_id