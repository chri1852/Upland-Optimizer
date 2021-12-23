﻿CREATE PROCEDURE [UPL].[AddRegisteredUserSendUPX]
(
	@DiscordUserId DECIMAL(20,0),
	@SendUPX INT
)
AS
BEGIN
	BEGIN TRY		
		UPDATE [UPL].[RegisteredUser]
		SET SendUpx += @SendUPX
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