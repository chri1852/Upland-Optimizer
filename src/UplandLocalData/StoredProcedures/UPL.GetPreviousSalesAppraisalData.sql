CREATE PROCEDURE [UPL].[GetPreviousSalesAppraisalData]
AS
BEGIN
	BEGIN TRY		
		DECLARE @PreviousSale TABLE
		(
			Type VARCHAR(20),
			Id INT,
			Currency VARCHAR(3),
			PerUp2 Decimal(11,2)
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

		DECLARE @PreviousSalesData DATETIME
		SET @PreviousSalesData = DATEADD(WW,-4,GETDATE())

		INSERT INTO @PreviousSale
		SELECT 'STREET', P.StreetId, 'UPX', SUM(S.Amount)/ SUM(P.Size)
		FROM UPL.SaleHistory S (NOLOCK)
			JOIN UPL.Property P (NOLOCK)
				ON S.PropId = P.Id
			JOIN @ValidStreets VS
				ON P.StreetId = VS.Id
			LEFT JOIN UPL.PropertyStructure PS (NOLOCK)
				ON S.PropId = PS.PropertyId
		WHERE S.SellerEOS IS NOT NULL
			AND S.BuyerEOS IS NOT NULL
			AND S.OfferPropId IS NULL
			AND S.AmountFiat IS NULL
			AND S.DateTime >= @PreviousSalesData
			AND PS.StructureType IS NULL
		GROUP BY P.StreetId
		HAVING COUNT(*) > 25

		INSERT INTO @PreviousSale
		SELECT 'NEIGHBORHOOD', P.NeighborhoodId, 'UPX', SUM(S.Amount)/ SUM(P.Size)
		FROM UPL.SaleHistory S (NOLOCK)
			JOIN UPL.Property P (NOLOCK)
				ON S.PropId = P.Id
			LEFT JOIN UPL.PropertyStructure PS (NOLOCK)
				ON S.PropId = PS.PropertyId
		WHERE S.SellerEOS IS NOT NULL
			AND S.BuyerEOS IS NOT NULL
			AND S.OfferPropId IS NULL
			AND S.AmountFiat IS NULL
			AND S.DateTime >= @PreviousSalesData
			AND PS.StructureType IS NULL
			AND P.NeighborhoodId IS NOT NULL
		GROUP BY P.NeighborhoodId
		HAVING COUNT(*) > 25

		INSERT INTO @PreviousSale
		SELECT 'COLLECTION', CP.CollectionId, 'UPX', SUM(S.Amount)/ SUM(P.Size)
		FROM UPL.SaleHistory S (NOLOCK)
			JOIN UPL.Property P (NOLOCK)
				ON S.PropId = P.Id
			JOIN UPL.CollectionProperty CP (NOLOCK)
				ON P.Id = CP.PropertyId
			JOIN UPL.Collection C (NOLOCK)
				ON CP.CollectionId = C.Id
			LEFT JOIN UPL.PropertyStructure PS (NOLOCK)
				ON S.PropId = PS.PropertyId
		WHERE S.SellerEOS IS NOT NULL
			AND S.BuyerEOS IS NOT NULL
			AND S.OfferPropId IS NULL
			AND S.AmountFiat IS NULL
			AND S.DateTime >= @PreviousSalesData
			AND PS.StructureType IS NULL
			AND C.Category <= 3
		GROUP BY CP.CollectionId
		HAVING COUNT(*) > 25

		INSERT INTO @PreviousSale
		SELECT 'COLLECTION', CP.CollectionId, 'UPX', SUM(S.Amount)/ SUM(P.Size)
		FROM UPL.SaleHistory S (NOLOCK)
			JOIN UPL.Property P (NOLOCK)
				ON S.PropId = P.Id
			JOIN UPL.CollectionProperty CP (NOLOCK)
				ON P.Id = CP.PropertyId
			JOIN UPL.Collection C (NOLOCK)
				ON CP.CollectionId = C.Id
			LEFT JOIN UPL.PropertyStructure PS (NOLOCK)
				ON S.PropId = PS.PropertyId
		WHERE S.SellerEOS IS NOT NULL
			AND S.BuyerEOS IS NOT NULL
			AND S.OfferPropId IS NULL
			AND S.AmountFiat IS NULL
			AND S.DateTime >= DATEADD(WW,-4,@PreviousSalesData)
			AND PS.StructureType IS NULL
			AND C.Category = 4
		GROUP BY CP.CollectionId
		HAVING COUNT(*) > 25

		INSERT INTO @PreviousSale
		SELECT 'COLLECTION', CP.CollectionId, 'UPX', SUM(S.Amount)/ SUM(P.Size)
		FROM UPL.SaleHistory S (NOLOCK)
			JOIN UPL.Property P (NOLOCK)
				ON S.PropId = P.Id
			JOIN UPL.CollectionProperty CP (NOLOCK)
				ON P.Id = CP.PropertyId
			JOIN UPL.Collection C (NOLOCK)
				ON CP.CollectionId = C.Id
			LEFT JOIN UPL.PropertyStructure PS (NOLOCK)
				ON S.PropId = PS.PropertyId
		WHERE S.SellerEOS IS NOT NULL
			AND S.BuyerEOS IS NOT NULL
			AND S.OfferPropId IS NULL
			AND S.AmountFiat IS NULL
			AND S.DateTime >= DATEADD(WW,-8,@PreviousSalesData)
			AND PS.StructureType IS NULL
			AND C.Category = 5
		GROUP BY CP.CollectionId
		HAVING COUNT(*) > 25

		SELECT * FROM @PreviousSale
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
