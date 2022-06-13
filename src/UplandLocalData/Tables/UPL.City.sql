CREATE TABLE [UPL].[City] 
(
	[CityId]             INT           NOT NULL,
	[Name]               VARCHAR(200)  NOT NULL, 
	[SquareCoordinates]  VARCHAR(200)          ,
	[StateCode]          VARCHAR(5)            ,          
	[CountryCode]        VARCHAR(3)    NOT NULL 


	CONSTRAINT pk_CityId PRIMARY KEY(CityId)
)
GO