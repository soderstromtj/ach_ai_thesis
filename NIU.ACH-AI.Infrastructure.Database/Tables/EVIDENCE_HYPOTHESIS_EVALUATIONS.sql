CREATE TABLE [dbo].[EVIDENCE_HYPOTHESIS_EVALUATIONS]
(
	[evidence_hypothesis_evaluation_id] UNIQUEIDENTIFIER NOT NULL PRIMARY KEY, 
    [step_execution_id] UNIQUEIDENTIFIER NOT NULL,
    [hypothesis_id] UNIQUEIDENTIFIER NOT NULL, 
    [evidence_id] UNIQUEIDENTIFIER NOT NULL, 
    [evaluation_score_id] INT NOT NULL, 
    [rationale] NVARCHAR(MAX) NULL, 
    [confidence_score] DECIMAL(5, 4) NULL, 
    [confidence_rationale] NVARCHAR(MAX) NULL, 
    [created_at] DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_EHE_STEP_EXECUTION FOREIGN KEY (step_execution_id) REFERENCES STEP_EXECUTIONS(step_execution_id),
    CONSTRAINT FK_EHE_HYPOTHESIS FOREIGN KEY (hypothesis_id) REFERENCES HYPOTHESES(hypothesis_id),
    CONSTRAINT FK_EHE_EVIDENCE FOREIGN KEY (evidence_id) REFERENCES EVIDENCE(evidence_id),
    CONSTRAINT FK_EHE_EVALUATION_SCORE FOREIGN KEY (evaluation_score_id) REFERENCES EVALUATION_SCORES(evaluation_score_id)
)
