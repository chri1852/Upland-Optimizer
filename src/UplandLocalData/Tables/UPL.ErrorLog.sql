CREATE TABLE [UPL].[ErrorLog]
(
	[Id]                INT IDENTITY(1,1) NOT NULL,
	[Datetime]          DATETIME          NOT NULL,
	[Location]          VARCHAR(200)      NOT NULL,
	[Message]           VARCHAR(MAX)      NOT NULL

	CONSTRAINT pk_ErrorLogId PRIMARY KEY(Id)
)
GO