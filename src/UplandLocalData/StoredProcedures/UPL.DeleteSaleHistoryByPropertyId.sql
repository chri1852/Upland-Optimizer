CREATE PROCEDURE [UPL].[DeleteSaleHistoryByPropertyId]
(
	@PropertyId BIGINT
)
AS
BEGIN
	BEGIN TRY		
		DELETE 
		FROM [UPL].[SaleHistory]
		WHERE (BuyerEOS IS NULL OR SellerEOS IS NULL)
			AND (PropId = @PropertyId OR OfferPropId = @PropertyId)
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
