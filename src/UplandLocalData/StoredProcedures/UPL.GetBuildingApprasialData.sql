CREATE PROCEDURE [UPL].[GetBuildingApprasialData]
AS
BEGIN
	BEGIN TRY		
		DECLARE @PreviousSalesData DATETIME
		SET @PreviousSalesData = DATEADD(WW,-4,GETDATE())

		SELECT DISTINCT
			PS.StructureType, 
			ROUND(PERCENTILE_CONT(0.5) WITHIN GROUP (ORDER BY SH.Amount - (P.MonthlyEarnings*12/0.1728)) OVER (PARTITION BY PS.StructureType), -2) AS 'Median'
		FROM UPL.PropertyStructure PS (NOLOCK)
			JOIN UPL.SaleHistory SH (NOLOCK)
				ON PS.PropertyId = SH.PropId
			JOIN UPL.Property P (NOLOCK)
				ON PS.PropertyId = P.Id
		WHERE SH.SellerEOS IS NOT NULL
			AND SH.BuyerEOS IS NOT NULL
			AND SH.OfferPropId IS NULL
			AND SH.AmountFiat IS NULL
			AND SH.DateTime >= @PreviousSalesData
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