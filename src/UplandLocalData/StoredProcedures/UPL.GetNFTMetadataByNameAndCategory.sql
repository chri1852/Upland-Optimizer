CREATE PROCEDURE [UPL].[GetNFTMetadataByNameAndCategory]
(
	@Name     VARCHAR(2000),
	@Category VARCHAR(50)
)
AS
BEGIN
	BEGIN TRY		
		SELECT * 
		FROM [UPL].[NFTMetadata] (NOLOCK)
		WHERE Name = @Name
			AND Category = @Category
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