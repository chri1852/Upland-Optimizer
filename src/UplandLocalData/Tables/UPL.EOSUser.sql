CREATE TABLE [UPL].[EOSUser] 
(
	[EOSAccount]     VARCHAR(12) NOT NULL, 
	[UplandUsername] VARCHAR(50) NOT NULL,
	[Joined]         DATETIME    NOT NULL

	CONSTRAINT pk_EOSUse PRIMARY KEY([EOSAccount])
)
GO