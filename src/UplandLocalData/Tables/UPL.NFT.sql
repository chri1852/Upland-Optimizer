CREATE TABLE [UPL].[NFT]
(
	[DGoodId]         INT NOT NULL,
	[NFTMetadataId]   INT NOT NULL,
	[SerialNumber]	  INT NOT NULL,
	[Burned]	      BIT NOT NULL,
	[CreatedDateTime] DATETIME NOT NULL,
	[BurnedDateTime]  DATETIME,
	[FullyLoaded]     BIT NOT NULL,
	[Metadata]        VARBINARY(MAX) NOT NULL

	CONSTRAINT pk_NFTDGoodId PRIMARY KEY(DGoodId)
)
GO