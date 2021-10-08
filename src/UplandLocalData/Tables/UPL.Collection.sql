CREATE TABLE [UPL].[Collection] 
(
	[Id]                 INT           NOT NULL,
	[Name]               VARCHAR(200)  NOT NULL, 
	[Category]           INT           NOT NULL,
	[Boost]              DECIMAL(3,2)  NOT NULL,
	[NumberOfProperties] INT           NOT NULL,
	[Description]        VARCHAR(1000) NOT NULL,
	[Reward]             INT           NOT NULL,
	[CityId]             INT                   ,
	[IsCityCollection]   BIT           NOT NULL

	CONSTRAINT pk_CollectionId PRIMARY KEY(Id)
)
GO