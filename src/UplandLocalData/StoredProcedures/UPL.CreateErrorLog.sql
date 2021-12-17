CREATE PROCEDURE [UPL].[CreateErrorLog]
(
	@Datetime   DATETIME,
	@Location   VARCHAR(200),
	@Message    VARCHAR(MAX)
)
AS
BEGIN
	BEGIN TRY		
		INSERT INTO [UPL].[ErrorLog]
		(
			[Datetime],
			[Location],
			[Message]
		)
		VALUES
		(
			@Datetime,
			@Location,
			@Message
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