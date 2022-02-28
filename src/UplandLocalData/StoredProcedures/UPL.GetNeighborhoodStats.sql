CREATE PROCEDURE [UPL].[GetNeighborhoodStats]
AS
BEGIN
	BEGIN TRY		
		SELECT 
			N.Id,
			ISNULL(P.FSA, 0) AS 'FSA',
			ISNULL(P.[Status], 'Owned') AS 'Status',
			CASE WHEN PS.StructureType IS NULL THEN CAST(0 AS BIT) ELSE CAST(1 AS BIT) END AS 'IsBuilt',
			CASE WHEN P.[Status] IS NULL THEN 0 ELSE COUNT(*) END AS 'PropCount'
		FROM 
			UPL.Neighborhood N (NOLOCK)
				LEFT JOIN UPL.Property P (NOLOCK)
					ON N.Id = P.NeighborhoodId
				LEFT JOIN UPL.PropertyStructure PS (NOLOCK)
					ON P.Id = PS.PropertyId
		WHERE 
			N.Id IS NOT NULL
		GROUP BY 
			N.Id, 
			P.FSA, 
			P.[Status],
			CASE WHEN PS.StructureType IS NULL THEN CAST(0 AS BIT) ELSE CAST(1 AS BIT) END
		ORDER BY 
			N.Id, 
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