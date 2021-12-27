CREATE PROCEDURE [UPL].[GetPropertiesForSale_City]
(
	@CityId INT,
	@OnlyBuildings BIT
)
AS
BEGIN
	BEGIN TRY
		IF @OnlyBuildings = 1
			SELECT 
				SH.PropId,
				ISNULL(SH.Amount, SH.AmountFiat) AS 'Price',
				CASE 
					WHEN SH.Amount IS NULL THEN 'USD' 
					ELSE 'UPX' 
				END AS 'Currency',
				E.UplandUsername AS 'Owner'
			FROM UPL.SaleHistory SH (NOLOCK)
				JOIN UPL.Property P (NOLOCK)
					ON SH.PropId = P.Id
				JOIN UPL.EOSUser E (NOLOCK)
					ON SH.SellerEOS = E.EOSAccount
				JOIN UPL.PropertyStructure PS (NOLOCK)
					ON P.Id = PS.PropertyId
			WHERE SH.BuyerEOS IS NULL
				AND P.CityId = @CityId
		ELSE
			SELECT 
				SH.PropId,
				ISNULL(SH.Amount, SH.AmountFiat) AS 'Price',
				CASE 
					WHEN SH.Amount IS NULL THEN 'USD' 
					ELSE 'UPX' 
				END AS 'Currency',
				E.UplandUsername AS 'Owner'
			FROM UPL.SaleHistory SH (NOLOCK)
				JOIN UPL.Property P (NOLOCK)
					ON SH.PropId = P.Id
				JOIN UPL.EOSUser E (NOLOCK)
					ON SH.SellerEOS = E.EOSAccount
			WHERE SH.BuyerEOS IS NULL
				AND P.CityId = @CityId
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