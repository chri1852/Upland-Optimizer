CREATE TABLE [UPL].[AppraisalRun]
(
	[Id]                INT IDENTITY(1,1) NOT NULL,
	[RegisteredUserId]  INT               NOT NULL,
	[RequestedDateTime] DATETIME          NOT NULL,
	[Results]           VARBINARY(MAX)            ,

	CONSTRAINT pk_AppraisalRunId PRIMARY KEY(Id)
)
GO