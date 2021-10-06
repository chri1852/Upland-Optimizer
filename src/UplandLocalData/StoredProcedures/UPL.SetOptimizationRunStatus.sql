CREATE PROCEDURE [UPL].[SetOptimizationRunStatus]
(
	@Id      INT,
	@Status  VARCHAR(20),
	@Results VARBINARY(MAX)
)
AS
BEGIN
	BEGIN TRY		
		UPDATE [UPL].[OptimizationRun]
		SET [Status] = @Status,
			[Results] = @Results
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