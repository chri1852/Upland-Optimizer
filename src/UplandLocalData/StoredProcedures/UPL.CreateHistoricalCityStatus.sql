CREATE PROCEDURE [UPL].[CreateHistoricalCityStatus]
(
	@CityId              INT         ,
	@TotalProps          INT         ,
	@Locked              INT         ,
	@UnlockedNonFSA      INT         ,
	@UnlockedFSA         INT         ,
	@ForSale             INT         ,
	@Owned               INT         ,
	@PercentMinted       DECIMAL(3,2),
	@PercentMintedNonFSA DECIMAL(3,2)          
)
AS
BEGIN
	BEGIN TRY		
		INSERT INTO [UPL].[HistoricalCityStatus]
		(
			[CityId], 
			[TotalProps],
			[Locked],
			[UnlockedNonFSA],
			[UnlockedFSA],
			[ForSale],
			[Owned],
			[PercentMinted],
			[PercentMintedNonFSA],
			[TimeStamp]
		)
		VALUES
		(
			@CityId,
			@TotalProps,
			@Locked,
			@UnlockedNonFSA,
			@UnlockedFSA,
			@ForSale,
			@Owned,
			@PercentMinted,
			@PercentMintedNonFSA,
			GETDATE()
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
