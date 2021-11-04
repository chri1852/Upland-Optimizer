CREATE PROCEDURE [UPL].[CreateStreet]
(
	@Id                 INT          ,
	@Name               VARCHAR(200) ,
	@Type               VARCHAR(50)  ,
	@CityId             INT             
)
AS
BEGIN
	BEGIN TRY		
		INSERT INTO [UPL].[Street]
		(
			[Id],
			[Name], 
			[Type],
			[CityId]
		)
		VALUES
		(
			@Id,
			@Name,
			@Type,
			@CityId
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