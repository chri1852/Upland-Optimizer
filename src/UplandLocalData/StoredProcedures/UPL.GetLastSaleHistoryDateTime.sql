CREATE PROCEDURE [UPL].[GetLastSaleHistoryDateTime]
AS
BEGIN
	BEGIN TRY		
		SELECT TOP(1) [DateTime]
		FROM [UPL].[SaleHistory] (NOLOCK)
		ORDER BY [DateTime] DESC
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