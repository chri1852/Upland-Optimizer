CREATE INDEX sh_propId
ON UPL.SaleHistory (PropId)
GO
CREATE INDEX prop_owner
ON UPL.Property (Owner)
GO
CREATE INDEX prop_cityId_Address
ON UPL.Property (CityId, Address)
GO
CREATE INDEX eu_uplandusername
ON UPL.EOSUser (UplandUsername)
GO