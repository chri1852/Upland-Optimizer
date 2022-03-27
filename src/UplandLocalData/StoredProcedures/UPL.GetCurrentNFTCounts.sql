CREATE PROCEDURE [UPL].[GetCurrentNFTCounts]
AS
BEGIN
	BEGIN TRY		
		SELECT M.Id, COUNT(*) AS 'Count'
		FROM [UPL].[NFTMetadata] M (NOLOCK)
			LEFT JOIN [UPL].[NFT] N (NOLOCK)
				ON M.Id = N.NFTMetadataId
		GROUP BY M.Id
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