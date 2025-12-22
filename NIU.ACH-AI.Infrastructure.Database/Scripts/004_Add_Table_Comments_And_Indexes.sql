/*
    Script: Add inline table comments and indexes to all CREATE TABLE scripts
    Date: 2025-12-22
    Purpose: Document table purposes and add performance indexes

    This script contains the updated CREATE TABLE definitions with:
    - Inline comments explaining table and column purposes
    - Indexes for common query patterns
    - Improved formatting

    NOTE: This file serves as a reference. The actual CREATE TABLE scripts in /Tables/
    should be updated manually by replacing their contents with the sections below.
*/

-- =====================================================================================
-- REFERENCE DATA TABLES (Lookups)
-- =====================================================================================

-- ACH_STEPS: Defines the steps in the ACH methodology
-- ---------------------------------------------------------------------------
-- This table stores the reference data for ACH steps. primary_ach_step groups
-- substeps under a main ACH step (e.g., steps 1a, 1b, 1c all have primary_ach_step=1)
CREATE TABLE [dbo].[ACH_STEPS]
(
	[ach_step_id] INT NOT NULL PRIMARY KEY,
    [step_name] NVARCHAR(100) NOT NULL,  -- Name of the ACH step
    [step_order] INT NOT NULL,  -- Execution order within the methodology
    [description] NVARCHAR(500) NOT NULL,  -- Description of what this step does
    [primary_ach_step] INT NOT NULL,  -- Groups substeps (1a, 1b, 1c -> all =1). Not a FK.
    CONSTRAINT [UQ_ACH_STEPS_step_name] UNIQUE ([step_name])
)
GO

CREATE NONCLUSTERED INDEX [IX_ACH_STEPS_primary_ach_step]
    ON [dbo].[ACH_STEPS]([primary_ach_step])
    INCLUDE ([step_name], [step_order]);
GO

CREATE NONCLUSTERED INDEX [IX_ACH_STEPS_step_order]
    ON [dbo].[ACH_STEPS]([step_order]);
GO

-- EVALUATION_SCORES: Reference table for evidence-hypothesis evaluation scores
-- ---------------------------------------------------------------------------
CREATE TABLE [dbo].[EVALUATION_SCORES]
(
	[evaluation_score_id] INT NOT NULL PRIMARY KEY IDENTITY(1,1),
    [score_name] NVARCHAR(50) NOT NULL,  -- e.g., "Consistent", "Inconsistent", "Neutral"
    [score_value] INT NOT NULL,  -- Numeric value for scoring (e.g., +1, 0, -1)
    [description] NVARCHAR(255) NULL  -- Explanation of what this score means
)
GO

CREATE NONCLUSTERED INDEX [IX_EVALUATION_SCORES_score_name]
    ON [dbo].[EVALUATION_SCORES]([score_name]);
GO

-- EVIDENCE_TYPES: Reference table for types of evidence
-- ---------------------------------------------------------------------------
CREATE TABLE [dbo].[EVIDENCE_TYPES]
(
	[evidence_type_id] INT NOT NULL PRIMARY KEY IDENTITY(1,1),
	[evidence_type_name] NVARCHAR(50) NOT NULL,  -- e.g., "Fact", "Assumption"
	[description] NVARCHAR(255) NULL,  -- Explanation of this evidence type
	CONSTRAINT [UQ_EVIDENCE_TYPES_evidence_type_name] UNIQUE ([evidence_type_name])
)
GO

-- ORCHESTRATION_TYPES: Reference table for agent orchestration patterns
-- ---------------------------------------------------------------------------
CREATE TABLE [dbo].[ORCHESTRATION_TYPES]
(
	[orchestration_type_id] UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
    [description] NVARCHAR(50) NOT NULL  -- e.g., "Sequential", "Parallel", "Round-Robin"
)
GO

-- PROVIDERS: AI service providers (OpenAI, Anthropic, etc.)
-- ---------------------------------------------------------------------------
CREATE TABLE [dbo].[PROVIDERS]
(
	[provider_id] UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
    [provider_name] NVARCHAR(50) NOT NULL,  -- e.g., "OpenAI", "Anthropic"
    [description] NVARCHAR(255) NULL,  -- Additional provider information
    [is_active] BIT NOT NULL DEFAULT 1,  -- Whether this provider is currently active
    [created_at] DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
)
GO

CREATE NONCLUSTERED INDEX [IX_PROVIDERS_is_active]
    ON [dbo].[PROVIDERS]([is_active])
    INCLUDE ([provider_name]);
GO

CREATE NONCLUSTERED INDEX [IX_PROVIDERS_provider_name]
    ON [dbo].[PROVIDERS]([provider_name]);
GO

-- SCENARIOS: Test scenarios containing context for experiments
-- ---------------------------------------------------------------------------
CREATE TABLE [dbo].[SCENARIOS]
(
	[scenario_id] UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
    [context] NVARCHAR(MAX) NOT NULL  -- The full scenario text/context for analysis
)
GO

/*
    The remaining tables (transactional tables) have already been updated in their individual files:
    - AGENT_CONFIGURATIONS (with indexes)
    - AGENT_RESPONSES (with indexes)
    - EVIDENCE (with indexes)
    - EVIDENCE_HYPOTHESIS_EVALUATIONS (with indexes)
    - EXPERIMENTS (with indexes)
    - HYPOTHESES (with indexes)
    - MODELS (with indexes)
    - STEP_EXECUTIONS (with indexes)
*/
