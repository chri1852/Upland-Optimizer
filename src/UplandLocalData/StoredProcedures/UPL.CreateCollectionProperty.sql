CREATE PROCEDURE [UPL].[CreateCollectionProperty]
(
	@CollectionId INT,
	@PropertyId   BIGINT
)
AS
BEGIN
	BEGIN TRY		
		INSERT INTO [UPL].[CollectionProperty]
		(
			[CollectionId],
			[PropertyId]
		)
		VALUES
		(
			@CollectionId,
			@PropertyId
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