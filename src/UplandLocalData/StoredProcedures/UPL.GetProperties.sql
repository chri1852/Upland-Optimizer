CREATE PROCEDURE [UPL].[GetProperties]
(
	@PropertyIds   [UPL].[PropertyTable] READONLY
)
AS
BEGIN
	BEGIN TRY		
		SELECT P.* 
		FROM [UPL].[Property] P 
			JOIN @PropertyIds PD
				ON P.Id = PD.PropertyId
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