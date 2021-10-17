CREATE PROCEDURE [UPL].[CreateNeighborhood]
(
	@Id                 INT          ,
	@Name               VARCHAR(200) ,
	@CityId             INT          ,
	@Coordinates        VARCHAR(MAX)    
)
AS
BEGIN
	BEGIN TRY		
		INSERT INTO [UPL].[Neighborhood]
		(
			[Id],
			[Name], 
			[CityId],
			[Coordinates]
		)
		VALUES
		(
			@Id,
			@Name,
			@CityId,
			@Coordinates
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