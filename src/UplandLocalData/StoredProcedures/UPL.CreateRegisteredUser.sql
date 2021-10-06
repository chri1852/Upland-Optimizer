CREATE PROCEDURE [UPL].[CreateRegisteredUser]
(
	@DiscordUserId   DECIMAL(20,0),
	@DiscordUsername VARCHAR(200),
	@UplandUsername  VARCHAR(200),
	@PropertyId      BIGINT,
	@Price           INT
)
AS
BEGIN
	BEGIN TRY		
		INSERT INTO [UPL].[RegisteredUser]
		(
			[DiscordUserId],
			[DiscordUserName], 
			[UplandUsername],
			[RunCount],
			[Paid],
			[PropertyId],
			[Price],
			[Verified]
		)
		VALUES
		(
			@DiscordUserId,
			@DiscordUsername,
			@UplandUsername,
			0,
			0,
			@PropertyId,
			@Price,
			0
		)
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