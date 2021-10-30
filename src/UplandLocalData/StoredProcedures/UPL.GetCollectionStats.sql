CREATE PROCEDURE [UPL].[GetCollectionStats]
AS
BEGIN
	BEGIN TRY		
		SELECT 
			CP.CollectionId AS 'Id',
			P.FSA,
			P.[Status],
			COUNT(*) AS 'PropCount'
		FROM 
			UPL.Property P (NOLOCK)
				JOIN UPL.CollectionProperty CP (NOLOCK)
					ON P.Id = CP.PropertyId
		WHERE 
			CP.CollectionId IS NOT NULL
			AND [Status] IS NOT NULL
		GROUP BY 
			CP.CollectionId, 
			P.FSA, 
			P.[Status]
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