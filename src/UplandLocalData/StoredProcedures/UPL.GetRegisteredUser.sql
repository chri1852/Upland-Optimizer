CREATE PROCEDURE [UPL].[GetRegisteredUser]
(
	@DiscordUserId  DECIMAL(20,0)
)
AS
BEGIN
	BEGIN TRY		
		SELECT TOP(1) * 
		FROM [UPL].[RegisteredUser] (NOLOCK)
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