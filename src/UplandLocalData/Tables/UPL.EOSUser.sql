CREATE TABLE [UPL].[EOSUser] 
(
	[Id]             INT IDENTITY(1,1) NOT NULL,
	[EOSAccount]     VARCHAR(12) NOT NULL, 
	[UplandUsername] VARCHAR(50) NOT NULL,
	[Joined]         DATETIME    NOT NULL,
	[Spark]          DECIMAL(6,2) DEFAULT 0 NOT NULL,
)
GO