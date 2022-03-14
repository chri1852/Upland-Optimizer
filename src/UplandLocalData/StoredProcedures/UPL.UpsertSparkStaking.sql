CREATE PROCEDURE [UPL].[UpsertSparkStaking]
(
	@Id              INT,
	@DGoodId         INT,
	@EOSUserId	     INT,
	@Amount          DECIMAL(6,2),
	@Start           DATETIME,
	@End             DATETIME
)
AS
BEGIN
	BEGIN TRY	
		IF(NOT EXISTS(SELECT * FROM [UPL].[SparkStaking] (NOLOCK) WHERE [Id] = @Id))
			BEGIN
				INSERT INTO [UPL].[SparkStaking]
				(
					[DGoodId],
					[EOSUserId],
					[Amount],
					[Start],
					[End]
				)
				Values
				(
					@DGoodId,
					@EOSUserId,
					@Amount,
					@Start,
					@End
				)
			END
		ELSE
			BEGIN
				UPDATE [UPL].[SparkStaking]
				SET
					[DGoodId] = @DGoodId,
					[EOSUserId] = @EOSUserId,
					[Amount] = @Amount,
					[Start] = @Start,
					[End] = @End
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