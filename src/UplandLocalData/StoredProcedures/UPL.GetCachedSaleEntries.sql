CREATE PROCEDURE [UPL].[GetCachedSaleEntries]
(
	@CityIdSearch BIT,
	@SearchByCityId INT,
	@SearchByUsername VARCHAR(12),

	@NoSales BIT,
	@NoSwaps BIT,
	@NoOffers BIT,

	@Currency VARCHAR(3),
	@Address VARCHAR(200),
	@Username VARCHAR(50),

	@FromDate DATETIME,
    @ToDate DATETIME
)
AS
BEGIN
	BEGIN TRY	

		SELECT
			SH.DateTime,
			Seller.UplandUsername AS 'Seller', 
			Buyer.UplandUsername AS 'Buyer', 
			CASE
				WHEN SH.OfferPropId IS NOT NULL THEN NULL
				ELSE ISNULL(SH.Amount, SH.AmountFiat)
			END AS 'Price',
			CASE 
				WHEN SH.Amount IS NULL THEN 'USD' 
				ELSE 'UPX' 
			END AS 'Currency',
			SH.Offer,
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
			AND SH.[DateTime] > @FromDate
			AND SH.[DateTime] < @ToDate
			AND ((@NoSales = 0) OR (@NoSales = 1 AND SH.Offer = 1))
			AND ((@NoSwaps = 0) OR (@NoSwaps = 1 AND SH.OfferPropId IS NULL))
			AND ((@NoOffers = 0) OR (@NoOffers = 1 AND (SH.Offer = 0 OR SH.OfferPropId IS NOT NULL)))
			AND ((@Currency IS NULL) OR (@Currency = 'USD' AND SH.AmountFiat IS NOT NULL) OR (@Currency = 'UPX' AND SH.Amount IS NOT NULL) OR SH.OfferPropId IS NOT NULL)
			AND ((@Address IS NULL) OR (P.Address LIKE '%' + @Address + '%') OR (OP.Address LIKE '%' + @Address + '%'))
			AND ((@Username IS NULL) OR (Seller.UplandUsername LIKE '%' + @Username + '%') OR (Buyer.UplandUsername LIKE '%' + @Username + '%'))
			AND ((@CityIdSearch = 1 AND (P.CityId = @SearchByCityId OR OP.CityId = @SearchByCityId))
				OR (@CityIdSearch = 0 AND (Seller.UplandUsername = @SearchByUsername OR Buyer.UplandUsername = @SearchByUsername)))
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
