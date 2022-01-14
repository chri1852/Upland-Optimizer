CREATE PROCEDURE [UPL].[UpdateRegisteredUser]
(
	@Id                       INT,
	@DiscordUserId            DECIMAL(20,0),
	@DiscordUsername          VARCHAR(200),
	@UplandUsername           VARCHAR(200),
	@RunCount                 INT,
	@Paid                     BIT,
	@PropertyId               BIGINT,
	@Price                    INT,
	@SendUpx                  INT,
	@PasswordSalt             VARCHAR(64),
	@PasswordHash             VARCHAR(64),
	@DiscordVerified          BIT,
	@WebVerified              BIT,
	@VerifyType               VARCHAR(3),
	@VerifyExpirationDateTime DATETIME  
)
AS
BEGIN
	BEGIN TRY		
		UPDATE [UPL].[RegisteredUser]
		SET [DiscordUserId] = @DiscordUserId,
			[DiscordUsername] = @DiscordUsername,
			[UplandUsername] = @UplandUsername,
			[RunCount] = @RunCount,
			[Paid] = @Paid,
			[PropertyId] = @PropertyId,
			[Price] = @Price,
			[SendUPX] = @SendUpx,
			[PasswordSalt] = @PasswordSalt,
			[PasswordHash] = @PasswordHash,
			[DiscordVerified] = @DiscordVerified,
			[WebVerified] = @WebVerified,
			[VerifyType] = @VerifyType,
			[VerifyExpirationDateTime] = @VerifyExpirationDateTime
		WHERE [Id] = @Id
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