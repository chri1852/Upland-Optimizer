﻿CREATE PROCEDURE [UPL].[GetStreetStats]
AS
BEGIN
	BEGIN TRY		
		SELECT 
			P.StreetId AS 'Id',
			P.FSA,
			P.[Status],
			CASE WHEN PS.StructureType IS NULL THEN CAST(0 AS BIT) ELSE CAST(1 AS BIT) END AS 'IsBuilt',
			COUNT(*) AS 'PropCount'
		FROM 
			UPL.Property P (NOLOCK)
			LEFT JOIN UPL.PropertyStructure PS (NOLOCK)
				ON P.Id = PS.PropertyId
		WHERE 
			P.StreetId IS NOT NULL
			AND P.[Status] IS NOT NULL
		GROUP BY 
			P.StreetId, 
			P.FSA, 
			P.[Status],
			CASE WHEN PS.StructureType IS NULL THEN CAST(0 AS BIT) ELSE CAST(1 AS BIT) END
		ORDER BY 
			P.StreetId, 
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