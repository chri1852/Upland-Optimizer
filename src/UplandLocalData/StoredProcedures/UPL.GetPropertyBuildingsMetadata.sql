CREATE PROCEDURE [UPL].[GetPropertyBuildingsMetadata]
AS
BEGIN
	BEGIN TRY		
		DECLARE @BuildingsInProgress TABLE
		(
			DGoodId INT
		)

		INSERT INTO @BuildingsInProgress
		SELECT DISTINCT DGoodId
		FROM UPL.SparkStaking H (NOLOCK)
		WHERE H.[End] IS NULL
			AND H.Manufacturing = 0
		
		SELECT DISTINCT N.Metadata AS 'NFTMetadata', M.Metadata
		FROM UPL.NFTMetadata M (NOLOCK)
			JOIN UPL.NFT N (NOLOCK)
				ON M.Id = N.NFTMetadataId
					AND M.Category = 'structure'
					AND N.Burned = 0
					AND N.FullyLoaded = 1
			LEFT JOIN @BuildingsInProgress H
				ON N.DGoodId = H.DGoodId
		WHERE H.DGoodId IS NULL
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