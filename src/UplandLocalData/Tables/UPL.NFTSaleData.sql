﻿CREATE TABLE [UPL].[NFTSaleData]
(
	[Id]               INT IDENTITY(1,1) NOT NULL,
	[DGoodId]          INT NOT NULL,
	[SellerEOS]        VARCHAR(12),
	[BuyerEOS]         VARCHAR(12),
	[Amount]           DECIMAL(11,2),
	[AmountFiat]       DECIMAL(11,2),
	[DateTime]         DATETIME,
	[SubMerchant]      VARCHAR(12),
	[SubMerchantFee]   DECIMAL(11,2)

	CONSTRAINT pk_NFTSaleDataId PRIMARY KEY(Id)
)
GO