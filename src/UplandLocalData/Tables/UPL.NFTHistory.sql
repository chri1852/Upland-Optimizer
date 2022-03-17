CREATE TABLE [UPL].[NFTHistory]
(
	[Id]               INT IDENTITY(1,1) NOT NULL,
	[DGoodId]          INT NOT NULL,
	[Owner]            VARCHAR(12),
	[ObtainedDateTime] DATETIME NOT NULL,
	[DisposedDateTime] DATETIME,

	CONSTRAINT pk_NFTHistoryId PRIMARY KEY(Id)
)
GO