CREATE PROCEDURE [UPL].[UpsertEOSUser]
(
	@EOSAccount      VARCHAR(12),
	@UplandUsername  VARCHAR(50)
)
AS
BEGIN
	BEGIN TRY	
		IF(NOT EXISTS(SELECT * FROM [UPL].[EOSUser] (NOLOCK) WHERE [EOSAccount] = @EOSAccount))
			BEGIN
				INSERT INTO [UPL].[EOSUser]
				(
					[EOSAccount],
					[UplandUsername]
				)
				Values
				(
					@EOSAccount,
					@UplandUsername
				)
			END
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