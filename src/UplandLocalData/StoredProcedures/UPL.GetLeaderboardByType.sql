CREATE PROCEDURE [UPL].[GetLeaderboardByType]
(
	@LeaderboardType INT,
	@AfterDateTime DATETIME
)
AS
BEGIN
	BEGIN TRY
	
		IF @LeaderboardType = 1
			BEGIN
				SELECT U.UplandUsername, (ISNULL(S.Staked, 0) + ISNULL(U.Spark, 0)) AS 'Total'
				FROM UPL.EOSUser U (NOLOCK)
					LEFT JOIN (SELECT EOSAccount, SUM(Amount) AS 'Staked' FROM UPL.SparkStaking (NOLOCK) WHERE [End] IS NULL GROUP BY EOSAccount) S
						ON U.EOSAccount = S.EOSAccount
				WHERE (ISNULL(S.Staked, 0) + ISNULL(U.Spark, 0)) > 0
				ORDER BY 'Total' DESC
			END

		ELSE IF @LeaderboardType = 3
			BEGIN
				SELECT U.UplandUsername, COUNT(*) AS 'Total'
				FROM UPL.EOSUser U (NOLOCK)
					JOIN UPL.Property P (NOLOCK)
						ON U.EOSAccount = P.Owner
				GROUP BY U.UplandUsername
				ORDER BY 'Total' DESC
			END

		ELSE IF @LeaderboardType = 4
			BEGIN
				SELECT UplandUsername, SUM(Mint*Boost) * 0.145152/12.0 AS 'Total'
				FROM UPL.Property (NOLOCK) 
					JOIN UPL.EOSUser (NOLOCK)
						ON Owner = EOSAccount
				GROUP BY UplandUsername
				ORDER BY 'Total' DESC
			END

		ELSE IF @LeaderboardType = 5
			BEGIN
				SELECT U.UplandUsername, SUM(P.Size) AS 'Total'
				FROM UPL.EOSUser U (NOLOCK)
					JOIN UPL.Property P (NOLOCK)
						ON U.EOSAccount = P.Owner
				GROUP BY U.UplandUsername
				ORDER BY 'Total' DESC
			END

		ELSE IF @LeaderboardType = 6
			BEGIN
				SELECT U.UplandUsername, SUM(P.Mint) AS 'Total'
				FROM UPL.EOSUser U (NOLOCK)
					JOIN UPL.Property P (NOLOCK)
						ON U.EOSAccount = P.Owner
				GROUP BY U.UplandUsername
				ORDER BY 'Total' DESC
			END

		ELSE IF @LeaderboardType = 7
			BEGIN
				SELECT U.UplandUsername, SUM(H.AmountFiat) AS 'Total'
				FROM UPL.SaleHistory H (NOLOCK)
					JOIN UPL.EOSUser U (NOLOCK)
						ON H.SellerEOS = U.EOSAccount
				WHERE H.BuyerEOS IS NOT NULL
					AND H.DateTime > @AfterDateTime
				GROUP BY U.UplandUsername
				HAVING SUM(H.AmountFiat) > 0
				ORDER BY SUM(H.AmountFiat) DESC
			END

		ELSE IF @LeaderboardType = 8
			BEGIN
				SELECT U.UplandUsername, SUM(H.Amount) AS 'Total'
				FROM UPL.SaleHistory H (NOLOCK)
					JOIN UPL.EOSUser U (NOLOCK)
						ON H.SellerEOS = U.EOSAccount
				WHERE H.BuyerEOS IS NOT NULL
					AND H.DateTime > @AfterDateTime
				GROUP BY U.UplandUsername
				HAVING SUM(H.Amount) > 0
				ORDER BY SUM(H.Amount) DESC
			END

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
