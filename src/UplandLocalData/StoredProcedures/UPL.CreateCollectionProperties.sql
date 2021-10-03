CREATE PROCEDURE [UPL].[CreateCollectionProperties]
(
	@CollectionId  INT,
	@PropertyIds   [UPL].[PropertyTable] READONLY
)
AS
BEGIN
	BEGIN TRY		
		INSERT INTO [UPL].[CollectionProperty]
		(
			[CollectionId],
			[PropertyId]
		)
		SELECT @CollectionId, *
		FROM @PropertyIds
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