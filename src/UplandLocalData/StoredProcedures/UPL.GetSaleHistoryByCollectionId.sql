﻿CREATE PROCEDURE [UPL].[GetSaleHistoryByCollectionId]
(
	@CollectionId  INT
)
AS
BEGIN
	BEGIN TRY		
		SELECT 
			S.DateTime, 
			Seller.UplandUsername AS 'Seller', 
			Buyer.UplandUsername AS 'Buyer',
			S.Offer,
			P.CityId, 
			P.Address, 
			P.Mint,
			CASE
				WHEN S.Amount IS NULL THEN S.AmountFiat
				ELSE S.Amount 
			END AS 'Price',
			CASE 
				WHEN S.Amount IS NULL THEN 'USD' 
				ELSE 'UPX' 
			END AS 'Currency',
			CASE
				WHEN S.Amount IS NULL THEN CONVERT(DECIMAL(10,2), S.AmountFiat*1000 / P.Mint)
				ELSE CONVERT(DECIMAL(10,2),S.Amount / P.Mint)
			END AS 'Markup'
		FROM UPL.SaleHistory S (NOLOCK)
			JOIN UPL.Property P (NOLOCK)
				ON S.PropId = P.Id
			JOIN UPL.EOSUser Seller (NOLOCK)
				ON S.SellerEOS = Seller.EOSAccount
			JOIN UPL.EOSUser Buyer (NOLOCK)
				ON S.BuyerEOS = Buyer.EOSAccount
			JOIN UPL.CollectionProperty CP (NOLOCK)
				ON P.Id = CP.PropertyId
		WHERE SellerEOS IS NOT NULL
			AND BuyerEOS IS NOT NULL
			AND OfferPropId IS NULL
			AND P.Mint != 0
			AND CP.CollectionId = @CollectionId
		ORDER BY S.DateTime DESC
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
GO