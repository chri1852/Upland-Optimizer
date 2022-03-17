CREATE PROCEDURE [UPL].[UpsertNFT]
(
	@DGoodId         INT,
	@NFTMetaDataId   INT,
	@SerialNumber    INT,
	@Burned          BIT,
	@CreatedDateTime DATETIME,
	@BurnedDateTime  DATETIME,
	@Metadata        VARBINARY(MAX)
)
AS
BEGIN
	BEGIN TRY	
		IF(NOT EXISTS(SELECT * FROM [UPL].[NFT] (NOLOCK) WHERE [DGoodId] = @DGoodId))
			BEGIN
				INSERT INTO [UPL].[NFT]
				(
					[DGoodId],
					[NFTMetadataId],
					[SerialNumber],
					[Burned],
					[CreatedDateTime],
					[BurnedDateTime],
					[Metadata]
				)
				Values
				(
					@DGoodId,
					@NFTMetaDataId,
					@SerialNumber,
					@Burned,
					@CreatedDateTime,
					@BurnedDateTime,
					@Metadata
				)
			END
		ELSE
			BEGIN
				UPDATE [UPL].[NFT]
				SET
					[NFTMetadataId] = @NFTMetaDataId,
					[SerialNumber] = @SerialNumber,
					[Burned] = @Burned,
					[CreatedDateTime] = @CreatedDateTime,
					[BurnedDateTime] = @BurnedDateTime,
					[Metadata] = @Metadata
				WHERE [DGoodId] = @DGoodId
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