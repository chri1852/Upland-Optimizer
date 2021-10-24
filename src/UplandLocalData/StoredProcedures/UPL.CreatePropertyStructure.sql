CREATE PROCEDURE [UPL].[CreatePropertyStructure]
(
	@PropertyId    BIGINT,
	@StructureType VARCHAR(100)
)
AS
BEGIN
	BEGIN TRY		
		INSERT INTO [UPL].[PropertyStructure]
		(
			[PropertyId],
			[StructureType]
		)
		VALUES
		(
			@PropertyId,
			@StructureType
		)
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