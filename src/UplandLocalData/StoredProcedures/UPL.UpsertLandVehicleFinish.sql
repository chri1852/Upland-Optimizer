CREATE PROCEDURE [UPL].[UpsertLandVehicleFinish]
(
	@Id               INT,
	@Title            VARCHAR(200),
	@Wheels           SMALLINT,
	@DriveTrain       VARCHAR(25),
	@MintingEnd       DATETIME,
	@CarClassId       INT,
	@CarClassName     VARCHAR(25),
	@CarClassNumber   INT,
	@Horsepower       INT,
	@Weight           INT,
	@Speed            SMALLINT,
	@Acceleration     SMALLINT,
	@Braking          SMALLINT,
	@Handling         SMALLINT,
	@EnergyEfficiency SMALLINT,
	@Reliability      SMALLINT,
	@Durability       SMALLINT,
	@Offroad          SMALLINT
)
AS
BEGIN
	BEGIN TRY	
		IF(NOT EXISTS(SELECT * FROM [UPL].[LandVehicleFinish] (NOLOCK) WHERE [Id] = @Id))
			BEGIN
				INSERT INTO [UPL].[LandVehicleFinish]
				(
					[Title], 
					[Wheels],
					[DriveTrain],          
					[MintingEnd],
					[CarClassId],
					[CarClassName],
					[CarClassNumber],
					[Horsepower],
					[Weight],
					[Speed],
					[Acceleration],
					[Braking],
					[Handling],
					[EnergyEfficiency],
					[Reliability],
					[Durability],
					[Offroad]
				)
				Values
				(
					@Title,
					@Wheels,
					@DriveTrain,
					@MintingEnd,
					@CarClassId,
					@CarClassName,
					@CarClassNumber,
					@Horsepower,
					@Weight,
					@Speed,
					@Acceleration,
					@Braking,
					@Handling,
					@EnergyEfficiency,
					@Reliability,
					@Durability,
					@Offroad
				)
			END
		ELSE
			BEGIN
				UPDATE [UPL].[LandVehicleFinish]
				SET
					[Title] = @Title, 
					[Wheels] = @Wheels,
					[DriveTrain] = @DriveTrain,          
					[MintingEnd] = @MintingEnd,
					[CarClassId] = @CarClassId,
					[CarClassName] = @CarClassName,
					[CarClassNumber] = @CarClassNumber,
					[Horsepower] = @Horsepower,
					[Weight] = @Weight,
					[Speed] = @Speed,
					[Acceleration] = @Acceleration,
					[Braking] = @Braking,
					[Handling] = @Handling,
					[EnergyEfficiency] = @EnergyEfficiency,
					[Reliability] = @Reliability,
					[Durability] = @Durability,
					[Offroad] = @Offroad
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