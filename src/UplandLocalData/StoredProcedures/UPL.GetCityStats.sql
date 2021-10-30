CREATE PROCEDURE [UPL].[GetCityStats]
AS
BEGIN
	BEGIN TRY		
		SELECT 
			CityId AS 'Id',
			FSA,
			[Status],
			COUNT(*) AS 'PropCount'
		FROM 
			UPL.Property (NOLOCK)
		WHERE 
			CityId IS NOT NULL
			AND [Status] IS NOT NULL
		GROUP BY 
			CityId, 
			FSA, 
			[Status]
		ORDER BY 
			CityId, 
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