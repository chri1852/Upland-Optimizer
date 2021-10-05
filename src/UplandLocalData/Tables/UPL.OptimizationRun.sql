CREATE TABLE [UPL].[OptimizationRun]
(
	[Id]                INT IDENTITY(1,1) NOT NULL,
	[DiscordUserId]     BIGINT            NOT NULL,
	[RequestedDateTime] DATETIME          NOT NULL,
	[Filename]          VARCHAR(200)      NOT NULL,
	[Status]            VARCHAR(20)       NOT NULL,

	CONSTRAINT pk_OptimizationRunId PRIMARY KEY(Id)
)
GO