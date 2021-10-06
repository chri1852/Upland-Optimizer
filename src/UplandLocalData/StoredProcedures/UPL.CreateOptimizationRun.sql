CREATE PROCEDURE [UPL].[CreateOptimizationRun]
(
	@DiscordUserId     DECIMAL(20,0),
	@RequestedDateTime DATETIME
)
AS
BEGIN
	BEGIN TRY		
		INSERT INTO [UPL].[OptimizationRun]
		(
			[DiscordUserId],
			[RequestedDateTime],
			[Status]
		)
		Values
		(
			@DiscordUserId,
			@RequestedDateTime,
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