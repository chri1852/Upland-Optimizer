CREATE PROCEDURE [UPL].[CreateAppraisalRun]
(
	@RegisteredUserId  INT,
	@RequestedDateTime DATETIME,
	@Results VARBINARY(MAX) 
)
AS
BEGIN
	BEGIN TRY
		INSERT INTO [UPL].[AppraisalRun]
		(
			[RegisteredUserId],
			[RequestedDateTime],
			[Results]
		)
		Values
		(
			@RegisteredUserId,
			@RequestedDateTime,
			@Results
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