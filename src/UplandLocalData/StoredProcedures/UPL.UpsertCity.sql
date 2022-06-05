CREATE PROCEDURE [UPL].[UpsertCity]
(
	@CityId            INT,
	@Name              VARCHAR(200),
	@SquareCoordinates VARCHAR(200),
	@StateCode         VARCHAR(5),
	@CountryCode       VARCHAR(3)
)
AS
BEGIN
	BEGIN TRY	
		IF(EXISTS(SELECT * FROM [UPL].[City] (NOLOCK) WHERE [CityId] = @CityId))
			BEGIN
				UPDATE [UPL].[City]
				SET
					[Name] = @Name,
					[SquareCoordinates] = @SquareCoordinates,
					[StateCode] = @StateCode,
					[CountryCode] = @CountryCode
				WHERE [CityId] = @CityId
			END
		ELSE
			BEGIN
				INSERT INTO [UPL].[City]
				(
					[CityId],
					[Name],
					[SquareCoordinates],
					[StateCode],
					[CountryCode]
				)
				Values
				(
					@CityId,
					@Name,
					@SquareCoordinates,
					@StateCode,
					@CountryCode
				)
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