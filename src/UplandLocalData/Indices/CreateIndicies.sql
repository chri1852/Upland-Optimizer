CREATE NONCLUSTERED INDEX sh_propId
ON UPL.SaleHistory (PropId)
GO
CREATE NONCLUSTERED INDEX prop_owner
ON UPL.Property (Owner)
GO
CREATE NONCLUSTERED INDEX prop_cityId_Address
ON UPL.Property (CityId, Address)
GO
CREATE NONCLUSTERED INDEX eu_uplandusername
ON UPL.EOSUser (UplandUsername)
GO
CREATE NONCLUSTERED INDEX prop_street
ON UPL.Property (StreetId)
GO
CREATE NONCLUSTERED INDEX prop_neighborhood
ON UPL.Property (NeighborhoodId)
GO
CREATE NONCLUSTERED INDEX prop_city
ON UPL.Property (CityId)
GO 
CREATE NONCLUSTERED INDEX sh_buyer
ON UPL.SaleHistory (BuyerEOS)
GO
CREATE NONCLUSTERED INDEX prop_status_cityStats
ON [UPL].[Property] ([Status])
INCLUDE ([CityId],[FSA])
GO
CREATE NONCLUSTERED INDEX collectionprop_CollectionId
ON [UPL].[CollectionProperty] ([CollectionId])
INCLUDE ([PropertyId])
GO
CREATE NONCLUSTERED INDEX sh_offerpropId_sellereos_buyereos
ON [UPL].[SaleHistory] ([OfferPropId],[SellerEOS],[BuyerEOS])
INCLUDE ([DateTime],[PropId],[Amount],[AmountFiat],[Offer])
GO
CREATE NONCLUSTERED INDEX prop_cityId_min
ON [UPL].[Property] ([CityId],[Mint])
INCLUDE ([Address])
GO
CREATE NONCLUSTERED INDEX propstruct_propId
ON [UPL].[PropertyStructure] ([PropertyId])
GO
CREATE NONCLUSTERED INDEX [p_web_for_sale_search_index]
ON [UPL].[Property] ([CityId],[NeighborhoodId],[Mint])
INCLUDE ([Address],[StreetId],[Size],[FSA])
GO
CREATE NONCLUSTERED INDEX [sh_web_for_sale_search_index]
ON [UPL].[SaleHistory] ([BuyerEOS])
INCLUDE ([SellerEOS],[PropId],[Amount],[AmountFiat])
GO
CREATE NONCLUSTERED INDEX [sh_web_sale_history]
ON [UPL].[SaleHistory] ([SellerEOS],[BuyerEOS])
INCLUDE ([DateTime],[PropId],[Amount],[AmountFiat],[OfferPropId],[Offer])
GO
CREATE NONCLUSTERED INDEX [nh_dgoodId]
ON [UPL].[NFTHistory] ([DGoodId])
GO
CREATE NONCLUSTERED INDEX [nh_disposedDateTime]
ON [UPL].[NFTHistory] ([DisposedDateTime])
GO