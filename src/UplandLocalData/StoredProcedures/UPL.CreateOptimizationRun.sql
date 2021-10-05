CREATE PROCEDURE [UPL].[CreateOptimizationRun]
(
	@DiscordUserId BIGINT,
	@RequestedDateTime DATETIME,
	@Filename VARCHAR(200)
)
AS
BEGIN
	BEGIN TRY		
		INSERT INTO [UPL].[OptimizationRun]
		(
			[DiscordUserId],
			[RequestedDateTime],
			[Filename],
			[Status]
		)
		Values
		(
			@DiscordUserId,
			@RequestedDateTime,
			@Filename,
			'Processing'
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