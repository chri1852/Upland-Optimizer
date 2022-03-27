CREATE PROCEDURE [UPL].[GetPropertyByCityIdAndAddress]
(
	@CityId INT,
	@Address VARCHAR(200)
)
AS
BEGIN
	BEGIN TRY		
		SELECT *
		FROM UPL.Property (NOLOCK)
		WHERE CityId = @CityId
			AND Address = @Address
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