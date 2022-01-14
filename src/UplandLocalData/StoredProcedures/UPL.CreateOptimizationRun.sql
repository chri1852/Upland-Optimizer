CREATE PROCEDURE [UPL].[CreateOptimizationRun]
(
	@RegisteredUserId  INT,
	@RequestedDateTime DATETIME
)
AS
BEGIN
	BEGIN TRY		
		INSERT INTO [UPL].[OptimizationRun]
		(
			[RegisteredUserId],
			[RequestedDateTime],
			[Status]
		)
		Values
		(
			@RegisteredUserId,
			@RequestedDateTime,
			'In Progress'
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