CREATE PROCEDURE [UPL].[GetCachedSaleEntriesSwaps]
AS
BEGIN
	BEGIN TRY	
		SELECT 
			SH.DateTime,
			Seller.UplandUsername AS 'Seller', 
			Buyer.UplandUsername AS 'Buyer', 
			P.Id,
			P.CityId, 
			P.Address, 
			P.NeighborhoodId, 
			P.Mint,
			OP.Id AS 'OfferProp_Id',
			OP.CityId AS 'OfferProp_CityId', 
			OP.Address AS 'OfferProp_Address', 
			OP.NeighborhoodId AS 'OfferProp_NeighborhoodId', 
			OP.Mint AS 'OfferProp_Mint'
		FROM UPL.SaleHistory SH (NOLOCK)
			JOIN UPL.Property P (NOLOCK)
				ON SH.PropId = P.Id
			LEFT JOIN UPL.Property OP (NOLOCK)
				ON SH.OfferPropId = OP.Id
			JOIN UPL.EOSUser Seller (NOLOCK)
				ON SH.SellerEOS = Seller.EOSAccount
			JOIN UPL.EOSUser Buyer (NOLOCK)
				ON SH.BuyerEOS = Buyer.EOSAccount
		WHERE (SH.SellerEOS IS NOT NULL AND SH.BuyerEOS IS NOT NULL)
			AND (SH.Offer = 1 AND SH.Accepted = 1 AND OfferPropId IS NOT NULL)
ORDER BY SH.[DateTime] DESC
	END TRY

	BEGIN CATCH
		DECLARE @ErrorMessage NVARCHAR(4000);
		DECLARE @ErrorSeverity INT;
		DECLARE @ErrorState INT;

		SELECT @ErrorMessage = ERROR_MESSAGE(),
			   @ErrorSeverity = ERROR_SEVERITY(),
			   @ErrorState = ERROR_STATE();

		RAISERROR (@ErrorMessage, @ErrorSeverity, @ErrorState)
	END CATCH
END
