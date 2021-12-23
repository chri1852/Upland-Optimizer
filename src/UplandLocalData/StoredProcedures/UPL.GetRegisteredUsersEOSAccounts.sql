CREATE PROCEDURE [UPL].[GetRegisteredUsersEOSAccounts]
AS
BEGIN
	BEGIN TRY		
		SELECT R.DiscordUserId, R.UplandUsername, U.EOSAccount
		FROM UPL.RegisteredUser R (NOLOCK)
			LEFT JOIN UPL.EOSUser U (NOLOCK)
				ON R.UplandUsername = U.UplandUsername
		WHERE U.UplandUsername IS NOT NULL
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