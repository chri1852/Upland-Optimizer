CREATE TABLE [UPL].[ConfigurationValue]
(
	[Id]                INT IDENTITY(1,1) NOT NULL,
	[Name]              VARCHAR(200)      NOT NULL,
	[Value]             VARCHAR(MAX)      NOT NULL

	CONSTRAINT pk_ConfigId PRIMARY KEY(Id)
)
GO