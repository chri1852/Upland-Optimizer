﻿CREATE TABLE [UPL].[Neighborhood] 
(
	[Id]                 INT           NOT NULL,
	[Name]               VARCHAR(200)  NOT NULL, 
	[CityId]             INT           NOT NULL,
	[Coordinates]        VARCHAR(MAX)  NOT NULL,
	[RGB]                VARCHAR(11)


	CONSTRAINT pk_NeighborhoodId PRIMARY KEY(Id)
)
GO