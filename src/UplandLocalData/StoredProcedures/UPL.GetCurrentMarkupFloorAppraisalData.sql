CREATE PROCEDURE [UPL].[GetCurrentMarkupFloorAppraisalData]
AS
BEGIN
	BEGIN TRY		
		DECLARE @CurrentMarkupFloor TABLE
		(
			Type VARCHAR(20),
			Id INT,
			Currency VARCHAR(3),
			MarkUpFloor Decimal(11,2)
		)
		DECLARE @ValidStreets TABLE
		(
			Id INT
		)

		INSERT INTO @ValidStreets
		SELECT S.Id 
		FROM UPL.Street S (NOLOCK)
			JOIN UPL.Property P (NOLOCK)
				ON S.Id = P.StreetId
		GROUP BY S.Id
		HAVING COUNT(*) > 50

		INSERT INTO @CurrentMarkupFloor
		SELECT 'CITY', C.CityId, 'UPX', PH.Mku
		FROM (SELECT DISTINCT CityId FROM UPL.Property (NOLOCK)) C
			JOIN (
				SELECT P.CityId, MIN(H.Amount/P.Mint) AS 'Mku'
				FROM UPL.Property P (NOLOCK)
					JOIN UPL.SaleHistory H (NOLOCK)
						ON P.Id = H.PropId
					LEFT JOIN UPL.PropertyStructure PS (NOLOCK)
						ON H.PropId = PS.PropertyId
				WHERE P.Id = H.PropId
					AND H.BuyerEOS IS NULL
					AND H.SellerEOS IS NOT NULL
					AND H.OfferPropId IS NULL
					AND H.AmountFiat IS NULL 
					AND PS.StructureType IS NULL
					AND P.Mint > 0
				GROUP BY P.CityId
				HAVING COUNT(*) > 50
			) AS PH
				ON C.CityID = PH.CityId

		INSERT INTO @CurrentMarkupFloor
		SELECT 'STREET', S.Id, 'UPX', PH.Mku
		FROM UPL.Street S (NOLOCK)
			JOIN (
				SELECT P.StreetId, MIN(H.Amount/P.Mint) AS 'Mku'
				FROM UPL.Property P (NOLOCK)
					JOIN UPL.SaleHistory H (NOLOCK)
						ON P.Id = H.PropId
					JOIN @ValidStreets VS
						ON P.StreetId = VS.Id
					LEFT JOIN UPL.PropertyStructure PS (NOLOCK)
						ON H.PropId = PS.PropertyId
				WHERE P.Id = H.PropId
					AND H.BuyerEOS IS NULL
					AND H.SellerEOS IS NOT NULL
					AND H.OfferPropId IS NULL
					AND H.AmountFiat IS NULL 
					AND PS.StructureType IS NULL
					AND P.Mint > 0
				GROUP BY P.StreetId
				HAVING COUNT(*) > 5
			) AS PH
				ON PH.StreetId = S.Id

		INSERT INTO @CurrentMarkupFloor
		SELECT 'NEIGHBORHOOD', N.Id, 'UPX', PH.Mku
		FROM UPL.Neighborhood N (NOLOCK)
			JOIN (
				SELECT P.NeighborhoodId, MIN(H.Amount/P.Mint) AS 'Mku'
				FROM UPL.Property P (NOLOCK)
					JOIN UPL.SaleHistory H (NOLOCK)
						ON P.Id = H.PropId
					LEFT JOIN UPL.PropertyStructure PS (NOLOCK)
						ON H.PropId = PS.PropertyId
				WHERE P.Id = H.PropId
					AND H.BuyerEOS IS NULL
					AND H.SellerEOS IS NOT NULL
					AND H.OfferPropId IS NULL
					AND H.AmountFiat IS NULL 
					AND PS.StructureType IS NULL
					AND P.Mint > 0
				GROUP BY P.NeighborhoodId
				HAVING COUNT(*) > 5
			) AS PH
				ON PH.NeighborhoodId = N.Id

		INSERT INTO @CurrentMarkupFloor
		SELECT 'COLLECTION', C.Id, 'UPX', PH.Mku
		FROM UPL.Collection C (NOLOCK)
			JOIN (
				SELECT CP.CollectionId, MIN(H.Amount/P.Mint) AS 'Mku'
				FROM UPL.CollectionProperty CP (NOLOCK)
					JOIN UPL.Property P (NOLOCK)
						ON CP.PropertyId = P.Id
					JOIN UPL.SaleHistory H (NOLOCK)
						ON P.Id = H.PropId
					LEFT JOIN UPL.PropertyStructure PS (NOLOCK)
						ON H.PropId = PS.PropertyId
				WHERE P.Id = H.PropId
					AND H.BuyerEOS IS NULL
					AND H.SellerEOS IS NOT NULL
					AND H.OfferPropId IS NULL
					AND H.AmountFiat IS NULL 
					AND PS.StructureType IS NULL
					AND P.Mint > 0
				GROUP BY CP.CollectionId
				HAVING COUNT(*) > 5
			) AS PH
				ON PH.CollectionId = C.Id

		SELECT * FROM @CurrentMarkupFloor
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
