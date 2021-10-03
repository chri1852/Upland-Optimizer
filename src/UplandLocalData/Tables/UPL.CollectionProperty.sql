CREATE TABLE [UPL].[CollectionProperty]
(
	[Id]           INT IDENTITY(1,1) NOT NULL,
	[CollectionId] INT               NOT NULL,
	[PropertyId]   BIGINT            NOT NULL,

	CONSTRAINT pk_CollectionPropertyId PRIMARY KEY(Id),
	CONSTRAINT fk_CollectionProperty_Collection FOREIGN KEY(CollectionId)
		REFERENCES [UPL].[Collection](Id)
)
GO