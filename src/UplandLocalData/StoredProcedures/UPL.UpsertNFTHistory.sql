CREATE PROCEDURE [UPL].[UpsertNFTHistory]
(
	@Id               INT,
	@DGoodId          INT,
	@Owner            VARCHAR(12),
	@ObtainedDateTime DATETIME,
	@DisposedDateTime DATETIME
)
AS
BEGIN
	BEGIN TRY	
		IF(NOT EXISTS(SELECT * FROM [UPL].[NFTHistory] (NOLOCK) WHERE [Id] = @Id))
			BEGIN
				INSERT INTO [UPL].[NFTHistory]
				(
					[DGoodId],
					[Owner],
					[ObtainedDateTime],
					[DisposedDateTime]
				)
				Values
				(
					@DGoodId,
					@Owner,
					@ObtainedDateTime,
					@DisposedDateTime
				)
			END
		ELSE
			BEGIN
				UPDATE [UPL].[NFTHistory]
				SET
					[DGoodId] = @DGoodId,
					[Owner] = @Owner,
					[ObtainedDateTime] = @ObtainedDateTime,
					[DisposedDateTime] = @DisposedDateTime
				WHERE [Id] = @Id
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
GO