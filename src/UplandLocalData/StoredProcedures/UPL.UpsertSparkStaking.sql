CREATE PROCEDURE [UPL].[UpsertSparkStaking]
(
	@Id              INT,
	@DGoodId         INT,
	@EOSAccount	     VARCHAR(12),
	@Amount          DECIMAL(6,2),
	@Start           DATETIME,
	@End             DATETIME,
	@Manufacturing   BIT
)
AS
BEGIN
	BEGIN TRY	
		IF(NOT EXISTS(SELECT * FROM [UPL].[SparkStaking] (NOLOCK) WHERE [Id] = @Id))
			BEGIN
				INSERT INTO [UPL].[SparkStaking]
				(
					[DGoodId],
					[EOSAccount],
					[Amount],
					[Start],
					[End],
					[Manufacturing]
				)
				Values
				(
					@DGoodId,
					@EOSAccount,
					@Amount,
					@Start,
					@End,
					@Manufacturing
				)
			END
		ELSE
			BEGIN
				UPDATE [UPL].[SparkStaking]
				SET
					[DGoodId] = @DGoodId,
					[EOSAccount] = @EOSAccount,
					[Amount] = @Amount,
					[Start] = @Start,
					[End] = @End,
					[Manufacturing] = @Manufacturing
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