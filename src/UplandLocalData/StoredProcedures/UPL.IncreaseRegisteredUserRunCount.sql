CREATE PROCEDURE [UPL].[IncreaseRegisteredUserRunCount]
(
	@DiscordUserId DECIMAL(20,0)
)
AS
BEGIN
	BEGIN TRY		
		UPDATE [UPL].[RegisteredUser]
		SET RunCount += 1
		WHERE DiscordUserId = @DiscordUserId
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