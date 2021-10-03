CREATE PROCEDURE [UPL].[CreateCollection]
(
	@Id                 INT,
	@Name               VARCHAR(200),
	@Category           INT,
	@Boost              DECIMAL(3,2),
	@NumberOfProperties INT,
	@Description        VARCHAR(1000),
	@Reward             INT
)
AS
BEGIN
	BEGIN TRY		
		INSERT INTO [UPL].[Collection]
		(
			[Id],
			[Name], 
			[Category],
			[Boost],
			[NumberOfProperties],
			[Description],
			[Reward]
		)
		VALUES
		(
			@Id,
			@Name,
			@Category,
			@Boost,
			@NumberOfProperties,
			@Description,
			@Reward
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