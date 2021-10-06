CREATE PROCEDURE [UPL].[GetPropertyIdsForCollectionId]
(
	@CollectionId INT
)
AS
BEGIN
	BEGIN TRY		
		SELECT PropertyId 
		FROM [UPL].[CollectionProperty] (NOLOCK)
		WHERE CollectionId = @CollectionId
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