CREATE PROCEDURE [UPL].[GetCachedForSalePropertiesByCityId]
(
	@CityId INT
)
AS
BEGIN
	BEGIN TRY		
		SELECT 
			P.Id,
			P.Address,
			P.CityId,
			P.NeighborhoodId,
			P.StreetId,
			P.Size,
			P.FSA,
			ISNULL(SH.Amount, SH.AmountFiat) AS 'Price',
			CASE 
				WHEN SH.Amount IS NULL THEN 'USD' 
				ELSE 'UPX' 
			END AS 'Currency',
			E.UplandUsername AS 'Owner',
			P.Mint,
			CASE 
				WHEN SH.Amount IS NULL THEN SH.AmountFiat*1000/P.Mint
				ELSE SH.Amount/P.Mint
			END AS 'Markup',
			ISNULL(PS.StructureType, '') AS 'Building'
		FROM UPL.SaleHistory SH (NOLOCK)
			JOIN UPL.Property P (NOLOCK)
				ON SH.PropId = P.Id
			JOIN UPL.EOSUser E (NOLOCK)
				ON SH.SellerEOS = E.EOSAccount
			LEFT JOIN UPL.PropertyStructure PS (NOLOCK)
				ON P.Id = PS.PropertyId
		WHERE SH.BuyerEOS IS NULL
			AND P.Mint > 0
			AND P.CityId = @CityId
			AND P.NeighborhoodId IS NOT NULL
			AND P.StreetId IS NOT NULL
			AND P.Size IS NOT NULL
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
