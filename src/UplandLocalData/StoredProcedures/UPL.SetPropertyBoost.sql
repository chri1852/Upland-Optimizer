CREATE  PROCEDURE [UPL].[SetPropertyBoost]
(
	@PropertyId      BIGINT,
	@Boost           DECIMAL(3,2)
)
AS
BEGIN
	BEGIN TRY	
		IF(EXISTS(SELECT * FROM [UPL].[Property] (NOLOCK) WHERE [Id] = @PropertyId))
			BEGIN
				UPDATE [UPL].[Property]
				SET
					[Boost] = @Boost
				WHERE [Id] = @PropertyId
			END
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