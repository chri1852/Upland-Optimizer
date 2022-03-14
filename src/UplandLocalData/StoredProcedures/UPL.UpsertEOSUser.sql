CREATE PROCEDURE [UPL].[UpsertEOSUser]
(
	@EOSAccount      VARCHAR(12),
	@UplandUsername  VARCHAR(50),
	@Joined          DATETIME,
	@Spark           DECIMAL(6,2)
)
AS
BEGIN
	BEGIN TRY	
		DELETE FROM [UPL].[EOSUser]
		WHERE [UplandUsername] = @UplandUsername

		INSERT INTO [UPL].[EOSUser]
		(
			[EOSAccount],
			[UplandUsername],
			[Joined],
			[Spark]
		)
		Values
		(
			@EOSAccount,
			@UplandUsername,
			@Joined,
			@Spark
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