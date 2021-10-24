CREATE TABLE [UPL].[PropertyStructure]
(
	[Id]            INT IDENTITY(1,1) NOT NULL,
	[PropertyId]    BIGINT            NOT NULL,
	[StructureType] VARCHAR(100)      NOT NULL,

	CONSTRAINT pk_PropertyStructureId PRIMARY KEY(Id),
	CONSTRAINT fk_PropertyStructure_Property FOREIGN KEY(PropertyId)
		REFERENCES [UPL].[Property](Id)
)
GO