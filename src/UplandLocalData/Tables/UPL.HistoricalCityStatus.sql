CREATE TABLE [UPL].[HistoricalCityStatus] 
(
	[Id]                  INT IDENTITY(1,1) NOT NULL,
	[CityId]              INT               NOT NULL,
	[TotalProps]          INT               NOT NULL, 
	[Locked]              INT               NOT NULL,
	[UnlockedNonFSA]      INT               NOT NULL,
	[UnlockedFSA]         INT               NOT NULL,
	[ForSale]             INT               NOT NULL,
	[Owned]               INT               NOT NULL,
	[PercentMinted]       DECIMAL(5,2)      NOT NULL,
	[PercentMintedNonFSA] DECIMAL(5,2)      NOT NULL,
	[TimeStamp]           DATETIME          NOT NULL,

	CONSTRAINT pk_HistoricalCityStatus PRIMARY KEY(Id)
)
GO