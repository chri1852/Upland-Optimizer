CREATE PROCEDURE [UPL].[GetAcquiredOnByPlayer]
(
	@UplandUsername VARCHAR(200)
)
AS
BEGIN
	BEGIN TRY		
		SELECT DISTINCT R.Id, R.Minted, R.AcquiredDateTime
		FROM (
			SELECT 
				P.Id, 
				CASE WHEN SH.DateTime IS NOT NULL THEN CAST(0 AS BIT) ELSE CAST(1 AS BIT) END AS 'Minted', 
				CASE WHEN SH.DateTime IS NOT NULL THEN SH.DateTime ELSE P.MintedOn END AS 'AcquiredDateTime', 
				RANK() OVER (PARTITION BY P.Id ORDER BY CASE WHEN SH.DateTime IS NOT NULL THEN SH.DateTime ELSE P.MintedOn END) date_rank
			FROM UPL.EOSUser U (NOLOCK)
				JOIN UPL.Property P (NOLOCK)
					ON U.EOSAccount = P.Owner
				LEFT JOIN UPL.SaleHistory SH (NOLOCK)
					ON SH.PropId = P.Id
						AND Sh.BuyerEOS = P.Owner
			WHERE U.UplandUsername = @UplandUsername
		) AS R
		WHERE R.date_rank = 1
ORDER BY R.AcquiredDateTime
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