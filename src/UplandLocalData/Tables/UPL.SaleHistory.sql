CREATE TABLE [UPL].[SaleHistory]
(
	[Id]                INT IDENTITY(1,1) NOT NULL,
	[DateTime]          DATETIME          NOT NULL,
	[SellerEOS]         VARCHAR(12)               ,
	[BuyerEOS]          VARCHAR(12)               ,
	[PropId]            BIGINT            NOT NULL,
	[Amount]            DECIMAL(11,2)             ,
	[OfferPropId]       BIGINT                    ,
	[Offer]             BIT               NOT NULL,
	[Accepted]          BIT               NOT NULL,

	CONSTRAINT pk_SaleHistoryId PRIMARY KEY([Id]),
	CONSTRAINT fk_SaleHistory_Property FOREIGN KEY([PropId])
		REFERENCES [UPL].[Property]([Id]),
	CONSTRAINT fk_SaleHistory_OfferProperty FOREIGN KEY([OfferPropId])
		REFERENCES [UPL].[Property]([Id]),
)
GO