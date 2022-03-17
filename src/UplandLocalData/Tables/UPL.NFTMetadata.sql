CREATE TABLE [UPL].[NFTMetadata]
(
	[Id]          INT IDENTITY(1,1) NOT NULL,
	[Name]	      VARCHAR(2000) NOT NULL,
	[Category]    VARCHAR(50) NOT NULL,
	[FullyLoaded] BIT NOT NULL,
	[Metadata]    VARBINARY(MAX) NOT NULL,

	CONSTRAINT pk_NFTMetadataId PRIMARY KEY(Id)
)
GO