CREATE TABLE [dbo].[ACH_STEPS]
(
	[ach_step_id] INT NOT NULL PRIMARY KEY, 
    [step_name] NVARCHAR(100) NOT NULL,
    [step_order] INT NOT NULL,
    [description] NVARCHAR(500) NOT NULL, 
    [primary_ach_step] INT NOT NULL, 
    CONSTRAINT [UQ_ACH_STEPS_step_name] UNIQUE ([step_name])
)
