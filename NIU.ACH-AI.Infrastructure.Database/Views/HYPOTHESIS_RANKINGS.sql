CREATE VIEW dbo.HYPOTHESIS_RANKINGS
AS
SELECT        dbo.STEP_EXECUTIONS.experiment_id AS Experiment_ID, h.hypothesis_id, h.short_title AS Hypothesis_ShortTitle, h.hypothesis_text AS Hypothesis, SUM(s.score_value) AS TotalScore, 
                         COUNT(CASE WHEN s.score_value < 0 THEN 1 END) AS InconsistentEvidenceCount, COUNT(CASE WHEN s.score_value > 0 THEN 1 END) AS ConsistentEvidenceCount, COUNT(CASE WHEN s.score_value = 0 THEN 1 END) 
                         AS NeutralEvidenceCount, COUNT(CASE WHEN s.score_value = 2 THEN 1 END) AS VeryConsistentEvidenceCount, COUNT(CASE WHEN s.score_value = - 2 THEN 1 END) AS VeryInconsistentEvidenceCount
FROM            dbo.HYPOTHESES AS h INNER JOIN
                         dbo.EVIDENCE_HYPOTHESIS_EVALUATIONS AS ehe ON h.hypothesis_id = ehe.hypothesis_id LEFT OUTER JOIN
                         dbo.EVALUATION_SCORES AS s ON ehe.evaluation_score_id = s.evaluation_score_id INNER JOIN
                         dbo.STEP_EXECUTIONS ON ehe.step_execution_id = dbo.STEP_EXECUTIONS.step_execution_id
GROUP BY h.hypothesis_id, h.short_title, h.hypothesis_text, dbo.STEP_EXECUTIONS.experiment_id
GO
EXECUTE sp_addextendedproperty @name = N'MS_DiagramPaneCount', @value = 2, @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'VIEW', @level1name = N'HYPOTHESIS_RANKINGS';


GO
EXECUTE sp_addextendedproperty @name = N'MS_DiagramPane2', @value = N'nd
', @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'VIEW', @level1name = N'HYPOTHESIS_RANKINGS';


GO
EXECUTE sp_addextendedproperty @name = N'MS_DiagramPane1', @value = N'[0E232FF0-B466-11cf-A24F-00AA00A3EFFF, 1.00]
Begin DesignProperties = 
   Begin PaneConfigurations = 
      Begin PaneConfiguration = 0
         NumPanes = 4
         Configuration = "(H (1[40] 4[20] 2[20] 3) )"
      End
      Begin PaneConfiguration = 1
         NumPanes = 3
         Configuration = "(H (1 [50] 4 [25] 3))"
      End
      Begin PaneConfiguration = 2
         NumPanes = 3
         Configuration = "(H (1 [50] 2 [25] 3))"
      End
      Begin PaneConfiguration = 3
         NumPanes = 3
         Configuration = "(H (4 [30] 2 [40] 3))"
      End
      Begin PaneConfiguration = 4
         NumPanes = 2
         Configuration = "(H (1 [56] 3))"
      End
      Begin PaneConfiguration = 5
         NumPanes = 2
         Configuration = "(H (2 [66] 3))"
      End
      Begin PaneConfiguration = 6
         NumPanes = 2
         Configuration = "(H (4 [50] 3))"
      End
      Begin PaneConfiguration = 7
         NumPanes = 1
         Configuration = "(V (3))"
      End
      Begin PaneConfiguration = 8
         NumPanes = 3
         Configuration = "(H (1[56] 4[18] 2) )"
      End
      Begin PaneConfiguration = 9
         NumPanes = 2
         Configuration = "(H (1 [75] 4))"
      End
      Begin PaneConfiguration = 10
         NumPanes = 2
         Configuration = "(H (1[66] 2) )"
      End
      Begin PaneConfiguration = 11
         NumPanes = 2
         Configuration = "(H (4 [60] 2))"
      End
      Begin PaneConfiguration = 12
         NumPanes = 1
         Configuration = "(H (1) )"
      End
      Begin PaneConfiguration = 13
         NumPanes = 1
         Configuration = "(V (4))"
      End
      Begin PaneConfiguration = 14
         NumPanes = 1
         Configuration = "(V (2))"
      End
      ActivePaneConfig = 0
   End
   Begin DiagramPane = 
      Begin Origin = 
         Top = 0
         Left = 0
      End
      Begin Tables = 
         Begin Table = "h"
            Begin Extent = 
               Top = 6
               Left = 38
               Bottom = 136
               Right = 236
            End
            DisplayFlags = 280
            TopColumn = 0
         End
         Begin Table = "ehe"
            Begin Extent = 
               Top = 22
               Left = 434
               Bottom = 152
               Right = 723
            End
            DisplayFlags = 280
            TopColumn = 0
         End
         Begin Table = "s"
            Begin Extent = 
               Top = 182
               Left = 39
               Bottom = 312
               Right = 247
            End
            DisplayFlags = 280
            TopColumn = 0
         End
         Begin Table = "STEP_EXECUTIONS"
            Begin Extent = 
               Top = 41
               Left = 958
               Bottom = 171
               Right = 1176
            End
            DisplayFlags = 280
            TopColumn = 0
         End
      End
   End
   Begin SQLPane = 
   End
   Begin DataPane = 
      Begin ParameterDefaults = ""
      End
      Begin ColumnWidths = 9
         Width = 284
         Width = 1500
         Width = 1500
         Width = 1500
         Width = 1500
         Width = 1500
         Width = 1500
         Width = 1500
         Width = 1500
      End
   End
   Begin CriteriaPane = 
      Begin ColumnWidths = 12
         Column = 1440
         Alias = 900
         Table = 1170
         Output = 720
         Append = 1400
         NewValue = 1170
         SortType = 1350
         SortOrder = 1410
         GroupBy = 1350
         Filter = 1350
         Or = 1350
         Or = 1350
         Or = 1350
      End
   End
E', @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'VIEW', @level1name = N'HYPOTHESIS_RANKINGS';

