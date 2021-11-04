CREATE PROCEDURE [UPL].[GetStreetStats]
AS
BEGIN
	BEGIN TRY		
		SELECT 
			StreetId AS 'Id',
			FSA,
			[Status],
			COUNT(*) AS 'PropCount'
		FROM 
			UPL.Property (NOLOCK)
		WHERE 
			StreetId IS NOT NULL
			AND [Status] IS NOT NULL
		GROUP BY 
			StreetId, 
			FSA, 
			[Status]
		ORDER BY 
			StreetId, 
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