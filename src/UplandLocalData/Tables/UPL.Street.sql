CREATE TABLE [UPL].[Street] 
(
	[Id]                 INT           NOT NULL,
	[Name]               VARCHAR(200)  NOT NULL, 
	[Type]               VARCHAR(50)   NOT NULL,
	[CityId]             INT           NOT NULL


	CONSTRAINT pk_StreetId PRIMARY KEY(Id)
)
GO