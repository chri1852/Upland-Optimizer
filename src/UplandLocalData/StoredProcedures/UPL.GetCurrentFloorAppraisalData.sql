CREATE PROCEDURE [UPL].[GetCurrentFloorAppraisalData]
AS
BEGIN
	BEGIN TRY		
		DECLARE @CurrentFloor TABLE
		(
			Type VARCHAR(20),
			Id INT,
			Currency VARCHAR(3),
			FloorValue Decimal(11,2)
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

		INSERT INTO @CurrentFloor
		SELECT 'STREET', S.Id, 'UPX', PH.Amt
		FROM UPL.Street S (NOLOCK)
			JOIN (
				SELECT P.StreetId, MIN(H.Amount) AS 'Amt'
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
				GROUP BY P.StreetId
				HAVING COUNT(*) > 5
			) AS PH
				ON PH.StreetId = S.Id

		INSERT INTO @CurrentFloor
		SELECT 'NEIGHBORHOOD', N.Id, 'UPX', PH.Amt
		FROM UPL.Neighborhood N (NOLOCK)
			JOIN (
				SELECT P.NeighborhoodId, MIN(H.Amount) AS 'Amt'
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
				GROUP BY P.NeighborhoodId
				HAVING COUNT(*) > 5
			) AS PH
				ON PH.NeighborhoodId = N.Id

		INSERT INTO @CurrentFloor
		SELECT 'COLLECTION', C.Id, 'UPX', PH.Amt
		FROM UPL.Collection C (NOLOCK)
			JOIN (
				SELECT CP.CollectionId, MIN(H.Amount) AS 'Amt'
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
				GROUP BY CP.CollectionId
				HAVING COUNT(*) > 5
			) AS PH
				ON PH.CollectionId = C.Id

		SELECT * FROM @CurrentFloor
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