CREATE TABLE [UPL].[SaleHistory]
(
	[Id]                INT IDENTITY(1,1) NOT NULL,
	[DateTime]          DATETIME          NOT NULL,
	[SellerEOS]         VARCHAR(12)               ,
	[BuyerEOS]          VARCHAR(12)               ,
	[PropId]            BIGINT            NOT NULL,
	[Amount]            DECIMAL(11,2)             ,
	[AmountFiat]        DECIMAL(11,2)             ,
	[OfferPropId]       BIGINT                    ,
	[Offer]             BIT               NOT NULL,
	[Accepted]          BIT               NOT NULL,

	CONSTRAINT pk_SaleHistoryId PRIMARY KEY([Id])
)
GO