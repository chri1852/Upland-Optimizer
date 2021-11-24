﻿CREATE PROCEDURE [UPL].[DeleteSaleHistoryById]
(
	@Id INT
)
AS
BEGIN
	BEGIN TRY		
		DELETE 
		FROM [UPL].[SaleHistory]
		WHERE Id = @Id
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