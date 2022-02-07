CREATE PROCEDURE [UPL].[CatchWhales]
AS
BEGIN
	BEGIN TRY		
		SELECT TOP(5000) U.UplandUsername, SUM(P.Mint) AS 'TotalMint'
		FROM UPL.EOSUser U (NOLOCK)
			JOIN UPL.Property P (NOLOCK)
				ON U.EOSAccount = P.Owner
		GROUP BY U.UplandUsername
		ORDER BY 'TotalMint' DESC
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
