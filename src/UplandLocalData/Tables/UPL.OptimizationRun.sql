CREATE TABLE [UPL].[OptimizationRun]
(
	[Id]                INT IDENTITY(1,1) NOT NULL,
	[DiscordUserId]     DECIMAL(20,0)     NOT NULL,
	[RequestedDateTime] DATETIME          NOT NULL,
	[Results]           VARBINARY(MAX)            ,
	[Status]            VARCHAR(20)       NOT NULL,

	CONSTRAINT pk_OptimizationRunId PRIMARY KEY(Id)
)
GO