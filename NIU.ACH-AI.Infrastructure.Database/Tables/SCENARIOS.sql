-- SCENARIOS: Test scenarios containing context/background information for experiments
-- Each scenario represents a situation to be analyzed using the ACH methodology
CREATE TABLE [dbo].[SCENARIOS]
(
	[scenario_id] UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
    [context] NVARCHAR(MAX) NOT NULL  -- Full scenario text/background for analysis
)
GO
