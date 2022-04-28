CREATE PROCEDURE [UPL].[GetNFTs]
(
	@DGoodIds [UPL].[NFTIdTable] READONLY
)
AS
BEGIN
	BEGIN TRY		
		SELECT N.*
		FROM [UPL].[NFT] N (NOLOCK)
			JOIN @DGoodIds D
				ON N.DGoodId = D.NFTMetadataId
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