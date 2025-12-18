CREATE TABLE [dbo].[EVIDENCE_TYPES]
(
	[evidence_type_id] INT NOT NULL PRIMARY KEY IDENTITY(1,1), 
	[evidence_type_name] NVARCHAR(50) NOT NULL, 
	[description] NVARCHAR(255) NULL, 
	CONSTRAINT [UQ_EVIDENCE_TYPES_evidence_type_name] UNIQUE ([evidence_type_name])
)
