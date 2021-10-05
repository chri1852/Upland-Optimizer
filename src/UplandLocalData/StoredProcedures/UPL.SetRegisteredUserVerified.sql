CREATE PROCEDURE [UPL].[SetRegisteredUserVerified]
(
	@UplandUsername  VARCHAR(200)
)
AS
BEGIN
	BEGIN TRY		
		UPDATE [UPL].[RegisteredUser]
		SET Verified = 1
		WHERE UplandUserName = @UplandUsername
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