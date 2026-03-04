CREATE VIEW [dbo].[STEP_EXECUTION_TIME_COST]
AS 
SELECT 
    e.experiment_name,
    a.primary_ach_step,
    a.step_name,
    COUNT(se.step_execution_id) AS execution_count,
    AVG(DATEDIFF(SECOND, se.datetime_start, se.datetime_end)) AS avg_duration_seconds,
    MAX(DATEDIFF(SECOND, se.datetime_start, se.datetime_end)) AS max_duration_seconds
FROM STEP_EXECUTIONS se
JOIN ACH_STEPS a ON se.ach_step_id = a.ach_step_id
JOIN EXPERIMENTS e ON se.experiment_id = e.experiment_id
WHERE a.primary_ach_step IN (1, 2, 3)
  AND se.execution_status = 'Completed'
GROUP BY 
    e.experiment_name, 
    a.primary_ach_step, 
    a.step_name;
