CREATE PROCEDURE [UPL].[DeleteOptimizerRuns]
(
	@RegisteredUserId INT
)
AS
BEGIN
	BEGIN TRY		
		DELETE 
		FROM [UPL].[OptimizationRun]
		WHERE RegisteredUserId = @RegisteredUserId
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