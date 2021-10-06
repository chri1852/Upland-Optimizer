CREATE PROCEDURE [UPL].[GetLatestOptimizationRunForDiscordUserId]
(
	@DiscordUserId DECIMAL(20,0)
)
AS
BEGIN
	BEGIN TRY		
		SELECT TOP(1) * 
		FROM [UPL].[OptimizationRun] (NOLOCK)
		WHERE DiscordUserId = @DiscordUserId
		ORDER BY RequestedDateTime DESC
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