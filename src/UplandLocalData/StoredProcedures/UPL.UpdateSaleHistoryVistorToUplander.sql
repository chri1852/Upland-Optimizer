CREATE PROCEDURE [UPL].[UpdateSaleHistoryVistorToUplander]
(
	@EOSAccount VARCHAR(12),
	@NewEOSAccount VARCHAR(12)

)
AS
BEGIN
	BEGIN TRY		
		UPDATE [UPL].[SaleHistory]
		SET BuyerEOS = @NewEOSAccount
		WHERE BuyerEOS = @EOSAccount

		UPDATE [UPL].[SaleHistory]
		SET SellerEOS = @NewEOSAccount
		WHERE SellerEOS = @EOSAccount
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
