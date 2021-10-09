CREATE PROCEDURE [UPL].[GetPropertiesByCollectionId]
(
	@CollectionId INT
)
AS
BEGIN
	BEGIN TRY		
		SELECT P.*
		FROM UPL.CollectionProperty CP (NOLOCK)
			JOIN UPL.Property P (NOLOCK)
				ON P.Id = CP.PropertyId
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