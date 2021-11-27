CREATE PROCEDURE [UPL].[UpsertConfigurationValue]
(
	@Name      VARCHAR(200),
	@Value     VARCHAR(MAX)
)
AS
BEGIN
	BEGIN TRY	
		IF(NOT EXISTS(SELECT * FROM [UPL].[ConfigurationValue] (NOLOCK) WHERE [Name] = @Name))
			BEGIN
				INSERT INTO [UPL].[ConfigurationValue]
				(
					[Name],
					[Value]
				)
				Values
				(
					@Name,
					@Value
				)
			END
		ELSE
			BEGIN
				UPDATE [UPL].[ConfigurationValue]
				SET
					[Value] = @Value
				WHERE [Name] = @Name
			END
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