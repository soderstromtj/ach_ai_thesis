-- EVIDENCE_TYPES: Reference table categorizing types of evidence
-- Examples: "Fact", "Assumption", "Observation", "Expert Opinion"
CREATE TABLE [dbo].[EVIDENCE_TYPES]
(
	[evidence_type_id] INT NOT NULL PRIMARY KEY IDENTITY(1,1),
	[evidence_type_name] NVARCHAR(50) NOT NULL,  -- e.g., "Fact", "Assumption"
	[description] NVARCHAR(255) NULL,  -- Explanation of this evidence type
	CONSTRAINT [UQ_EVIDENCE_TYPES_evidence_type_name] UNIQUE ([evidence_type_name])
)
GO
