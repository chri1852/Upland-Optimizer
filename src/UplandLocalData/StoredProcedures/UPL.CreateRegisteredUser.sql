CREATE PROCEDURE [UPL].[CreateRegisteredUser]
(
	@DiscordUserId             DECIMAL(20,0),
	@DiscordUsername           VARCHAR(200),
	@UplandUsername            VARCHAR(200),
	@PropertyId                BIGINT,
	@Price                     INT,
	@VerifyType                VARCHAR(3),
	@VerifyExpirationDateTime  DATETIME
)
AS
BEGIN
	BEGIN TRY		
		INSERT INTO [UPL].[RegisteredUser]
		(
			[DiscordUserId],
			[DiscordUsername], 
			[UplandUsername],
			[RunCount],
			[Paid],
			[PropertyId],
			[Price],
			[SendUPX],
			[PasswordSalt],
			[PasswordHash],
			[DiscordVerified],
			[WebVerified],
			[VerifyType],
			[VerifyExpirationDateTime]
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
			0,
			NULL,
			NULL,
			0,
			0,
			@VerifyType,
			@VerifyExpirationDateTime
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