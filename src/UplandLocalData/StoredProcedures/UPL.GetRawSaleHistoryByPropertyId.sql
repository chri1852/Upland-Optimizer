CREATE PROCEDURE [UPL].[GetRawSaleHistoryByPropertyId]
(
	@PropId BIGINT
)
AS
BEGIN
	BEGIN TRY		
		SELECT *
		FROM [UPL].[SaleHistory] (NOLOCK)
		WHERE PropId = @PropId
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
