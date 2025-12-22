/*
    Migration: Fix MODELS token pricing precision
    Date: 2025-12-22
    Purpose: Increase decimal precision for accurate LLM token pricing

    Current: DECIMAL(6,2) = XX,XXX.XX (only 2 decimal places)
    New:     DECIMAL(10,8) = XX.XXXXXXXX (8 decimal places for micro-pricing)

    Rationale: Modern LLM pricing requires higher precision
    Example: GPT-4 Turbo = $0.00001 per input token (needs 5 decimal places minimum)

    Alternative approach: Store as cost-per-million tokens with DECIMAL(8,2)
    This migration uses per-token pricing with high precision.
*/

-- Step 1: Update input_token_cost precision
IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.MODELS') AND name = 'input_token_cost')
BEGIN
    ALTER TABLE [dbo].[MODELS]
    ALTER COLUMN [input_token_cost] DECIMAL(10, 8) NULL;

    PRINT 'Updated input_token_cost to DECIMAL(10,8)';
END
GO

-- Step 2: Update cached_input_token_cost precision
IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.MODELS') AND name = 'cached_input_token_cost')
BEGIN
    ALTER TABLE [dbo].[MODELS]
    ALTER COLUMN [cached_input_token_cost] DECIMAL(10, 8) NULL;

    PRINT 'Updated cached_input_token_cost to DECIMAL(10,8)';
END
GO

-- Step 3: Update output_token_cost precision
IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.MODELS') AND name = 'output_token_cost')
BEGIN
    ALTER TABLE [dbo].[MODELS]
    ALTER COLUMN [output_token_cost] DECIMAL(10, 8) NULL;

    PRINT 'Updated output_token_cost to DECIMAL(10,8)';
END
GO

PRINT 'Successfully updated MODELS pricing precision to DECIMAL(10,8)';
GO

/*
    NOTE: If you prefer cost-per-million pricing instead, use this alternative:

    ALTER TABLE [dbo].[MODELS] ALTER COLUMN [input_token_cost] DECIMAL(8, 2) NULL;
    ALTER TABLE [dbo].[MODELS] ALTER COLUMN [cached_input_token_cost] DECIMAL(8, 2) NULL;
    ALTER TABLE [dbo].[MODELS] ALTER COLUMN [output_token_cost] DECIMAL(8, 2) NULL;

    Then rename columns:
    EXEC sp_rename 'dbo.MODELS.input_token_cost', 'input_cost_per_million', 'COLUMN';
    EXEC sp_rename 'dbo.MODELS.cached_input_token_cost', 'cached_input_cost_per_million', 'COLUMN';
    EXEC sp_rename 'dbo.MODELS.output_token_cost', 'output_cost_per_million', 'COLUMN';
*/
