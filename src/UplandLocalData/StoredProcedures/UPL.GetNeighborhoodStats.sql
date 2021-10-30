CREATE PROCEDURE [UPL].[GetNeighborhoodStats]
AS
BEGIN
	BEGIN TRY		
		SELECT 
			NeighborhoodId AS 'Id',
			FSA,
			[Status],
			COUNT(*) AS 'PropCount'
		FROM 
			UPL.Property (NOLOCK)
		WHERE 
			NeighborhoodId IS NOT NULL
			AND [Status] IS NOT NULL
		GROUP BY 
			NeighborhoodId, 
			FSA, 
			[Status]
		ORDER BY 
			NeighborhoodId, 
			[STATUS], 
			FSA
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