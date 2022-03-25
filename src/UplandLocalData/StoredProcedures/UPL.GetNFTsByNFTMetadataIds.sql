CREATE PROCEDURE [UPL].[GetNFTsByNFTMetadataIds]
(
	@NFTMetadataIds [UPL].[NFTIdTable] READONLY
)
AS
BEGIN
	BEGIN TRY		
		SELECT N.*, U.UplandUsername
		FROM UPL.NFT N (NOLOCK)
			JOIN @NFTMetadataIds M
				ON N.NFTMetadataId = M.NFTMetadataId
			LEFT JOIN UPL.NFTHistory H (NOLOCK)
				ON N.DGoodId = H.DGoodId
					AND H.DisposedDateTime IS NULL
			LEFT JOIN UPL.EOSUser U (NOLOCK)
				ON H.Owner = U.EOSAccount
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