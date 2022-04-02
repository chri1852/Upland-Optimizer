CREATE PROCEDURE [UPL].[GetNFTsByOwnerEOS]
(
	@EOSAccount VARCHAR(12)
)
AS
BEGIN
	BEGIN TRY		
		SELECT DISTINCT N.*, U.UplandUsername
		FROM UPL.NFTHistory H (NOLOCK)
			JOIN UPL.NFT N (NOLOCK)
				ON H.DGoodId = N.DGoodId
			JOIN UPL.EOSUser U (NOLOCK)
				ON H.Owner = U.EOSAccount
		WHERE H.Owner = @EOSAccount
			AND H.DisposedDateTime IS NULL
			AND N.Burned != 1
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