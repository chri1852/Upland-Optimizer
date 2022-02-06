CREATE PROCEDURE [UPL].[GetCachedSaleEntriesByCityId]
(
	@CityId INT
)
AS
BEGIN
	BEGIN TRY	
		SELECT 
			SH.DateTime,
			Seller.UplandUsername AS 'Seller', 
			Buyer.UplandUsername AS 'Buyer', 
			ISNULL(SH.Amount, SH.AmountFiat) AS 'Price',
			CASE 
				WHEN SH.Amount IS NULL THEN 'USD' 
				ELSE 'UPX' 
			END AS 'Currency',
			SH.Offer,
			P.Id,
			P.CityId, 
			P.Address, 
			P.NeighborhoodId, 
			P.Mint
		FROM UPL.SaleHistory SH (NOLOCK)
			JOIN UPL.Property P (NOLOCK)
				ON SH.PropId = P.Id
			JOIN UPL.EOSUser Seller (NOLOCK)
				ON SH.SellerEOS = Seller.EOSAccount
			JOIN UPL.EOSUser Buyer (NOLOCK)
				ON SH.BuyerEOS = Buyer.EOSAccount
		WHERE (SH.SellerEOS IS NOT NULL AND SH.BuyerEOS IS NOT NULL)
			AND ((SH.Offer = 0) OR (SH.Offer = 1 AND SH.Accepted = 1 AND OfferPropId IS NULL))
			AND P.CityId = @CityId
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
