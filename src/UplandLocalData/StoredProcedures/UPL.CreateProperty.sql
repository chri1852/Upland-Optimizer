CREATE PROCEDURE [UPL].[CreateProperty]
(
	@Id              BIGINT,
	@Address         VARCHAR(200),
	@CityId          INT,
	@StreetId        INT,
	@Size            INT,
	@MonthlyEarnings DECIMAL(11,2),
	@Latitude        DECIMAL(19,16),
	@Longitude       DECIMAL(19,16)
)
AS
BEGIN
	BEGIN TRY		
		INSERT INTO [UPL].[Property]
		(
			[Id],
			[Address],
			[CityId],
			[StreetId],
			[Size],
			[MonthlyEarnings],
			[Latitude],
			[Longitude]
		)
		Values
		(
			@Id,
			@Address,
			@CityId,
			@StreetId,
			@Size,
			@MonthlyEarnings,
			@Latitude,
			@Longitude   
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