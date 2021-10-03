CREATE TABLE [UPL].[Property] 
(
	[Id]              BIGINT        NOT NULL,
	[Address]         VARCHAR(200)  NOT NULL, 
	[CityId]          INT           NOT NULL,
	[StreetId]        INT           NOT NULL,
	[Size]            INT           NOT NULL,
	[MonthlyEarnings] DECIMAL(11,2) NOT NULL,


	CONSTRAINT pk_PropertyId PRIMARY KEY(Id)
)
GO