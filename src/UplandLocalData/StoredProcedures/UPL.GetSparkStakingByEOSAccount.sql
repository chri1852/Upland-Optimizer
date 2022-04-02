CREATE PROCEDURE [UPL].[GetSparkStakingByEOSAccount]
(
	@EOSAccount VARCHAR(12)
)
AS
BEGIN
	BEGIN TRY	
		SELECT *
		FROM [UPL].[SparkStaking] (NOLOCK)
		WHERE [EOSAccount] = @EOSAccount
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