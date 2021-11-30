CREATE PROCEDURE [UPL].[DeleteSaleHistoryByBuyerEOS]
(
	@EOSAccount VARCHAR(12)
)
AS
BEGIN
	BEGIN TRY		
		DELETE 
		FROM [UPL].[SaleHistory]
		WHERE BuyerEOS = @EOSAccount
			AND SellerEOS IS NULL
			AND Offer = 1
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