﻿CREATE TABLE [UPL].[Property] 
(
	[Id]              BIGINT        NOT NULL,
	[Address]         VARCHAR(200)  NOT NULL, 
	[CityId]          INT           NOT NULL,
	[StreetId]        INT           NOT NULL,
	[Size]            INT           NOT NULL,
	[MonthlyEarnings] DECIMAL(11,2) NOT NULL,
	[NeighborhoodId]  INT,
	[Latitude]        DECIMAL(19,3),
	[Longitude]       DECIMAL(19,3)


	CONSTRAINT pk_PropertyId PRIMARY KEY(Id)
)
GO