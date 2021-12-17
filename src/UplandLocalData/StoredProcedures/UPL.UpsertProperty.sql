﻿CREATE PROCEDURE [UPL].[UpsertProperty]
(
	@Id              BIGINT,
	@Address         VARCHAR(200),
	@CityId          INT,
	@StreetId        INT,
	@Size            INT,
	@MonthlyEarnings DECIMAL(11,2),
	@NeighborhoodId  INT = NULL,
	@Latitude        DECIMAL(19,16),
	@Longitude       DECIMAL(19,16),
	@Status          VARCHAR(25),
	@FSA             BIT,
	@Owner           VARCHAR(12) = NULL,
	@MintedOn        DATETIME = NULL,
	@MintedBy        VARCHAR(12) = NULL

)
AS
BEGIN
	BEGIN TRY	
		IF(EXISTS(SELECT * FROM [UPL].[Property] (NOLOCK) WHERE [Id] = @Id))
			BEGIN
				UPDATE [UPL].[Property]
				SET
					[Address] = @Address,
					[CityId] = @CityId,
					[StreetId] = @StreetId,
					[Size] = @Size,
					[MonthlyEarnings] = @MonthlyEarnings,
					[NeighborhoodId] = @NeighborhoodId,
					[Latitude] = @Latitude,
					[Longitude] = @Longitude,
					[Status] = @Status,
					[FSA] = @FSA,
					[Owner] = @Owner,
					[MintedOn] = @MintedOn,
					[MintedBy] = @MintedBy
				WHERE [Id] = @Id
			END
		ELSE
			BEGIN
				INSERT INTO [UPL].[Property]
				(
					[Id],
					[Address],
					[CityId],
					[StreetId],
					[Size],
					[MonthlyEarnings],
					[NeighborhoodId],
					[Latitude],
					[Longitude],
					[Status],
					[FSA],
					[Owner],
					[MintedOn],
					[MintedBy]
				)
				Values
				(
					@Id,
					@Address,
					@CityId,
					@StreetId,
					@Size,
					@MonthlyEarnings,
					@NeighborhoodId,
					@Latitude,
					@Longitude,
					@Status,
					@FSA,
					@Owner,
					@MintedOn,
					@MintedBy
				)
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