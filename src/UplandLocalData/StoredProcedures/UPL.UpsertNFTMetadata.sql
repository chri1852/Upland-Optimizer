CREATE PROCEDURE [UPL].[UpsertNFTMetadata]
(
	@Id       INT,
	@Name     VARCHAR(2000),
	@Category VARCHAR(50),
	@Metadata VARBINARY(MAX)
)
AS
BEGIN
	BEGIN TRY	
		IF(NOT EXISTS(SELECT * FROM [UPL].[NFTMetadata] (NOLOCK) WHERE [Id] = @Id))
			BEGIN
				INSERT INTO [UPL].[NFTMetadata]
				(
					[Name],
					[Category],
					[Metadata]
				)
				Values
				(
					@Name,
					@Category,
					@Metadata
				)
			END
		ELSE
			BEGIN
				UPDATE [UPL].[NFTMetadata]
				SET
					[Name] = @Name,
					[Category] = @Category,
					[Metadata] = @Metadata
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