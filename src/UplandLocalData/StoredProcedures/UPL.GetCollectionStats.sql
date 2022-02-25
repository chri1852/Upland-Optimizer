CREATE PROCEDURE [UPL].[GetCollectionStats]
AS
BEGIN
	BEGIN TRY		
		SELECT 
			CP.CollectionId AS 'Id',
			P.FSA,
			P.[Status],
			CASE WHEN PS.StructureType IS NULL THEN CAST(0 AS BIT) ELSE CAST(1 AS BIT) END AS 'IsBuilt',
			COUNT(*) AS 'PropCount'
		FROM 
			UPL.Property P (NOLOCK)
				JOIN UPL.CollectionProperty CP (NOLOCK)
					ON P.Id = CP.PropertyId
				LEFT JOIN UPL.PropertyStructure PS (NOLOCK)
					ON P.Id = PS.PropertyId
		WHERE 
			CP.CollectionId IS NOT NULL
			AND [Status] IS NOT NULL
		GROUP BY 
			CP.CollectionId, 
			P.FSA, 
			P.[Status],
			CASE WHEN PS.StructureType IS NULL THEN CAST(0 AS BIT) ELSE CAST(1 AS BIT) END
		ORDER BY 
			CP.CollectionId, 
			P.[STATUS], 
			P.FSA
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