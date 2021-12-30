CREATE TABLE [UPL].[Property] 
(
	[Id]              BIGINT        NOT NULL,
	[Address]         VARCHAR(200)  NOT NULL, 
	[CityId]          INT           NOT NULL,
	[StreetId]        INT           NOT NULL,
	[Size]            INT           NOT NULL,
	[Mint]            DECIMAL(11,2) NOT NULL,
	[NeighborhoodId]  INT,
	[Latitude]        DECIMAL(19,16),
	[Longitude]       DECIMAL(19,16),
	[Status]          VARCHAR(25),
	[FSA]             BIT,
	[Owner]           VARCHAR(12),
	[MintedOn]        DATETIME,
	[MintedBy]        VARCHAR(12)

	CONSTRAINT pk_PropertyId PRIMARY KEY(Id)
)
GO