CREATE TABLE [UPL].[LandVehicleFinish]
(
	[Id]               INT          NOT NULL,
	[Title]            VARCHAR(200) NOT NULL, 
	[Wheels]           SMALLINT     NOT NULL,
	[DriveTrain]       VARCHAR(25)  NOT NULL,          
	[MintingEnd]       DATETIME     NOT NULL,
	[CarClassId]       INT          NOT NULL,
	[CarClassName]     VARCHAR(25)  NOT NULL,
	[CarClassNumber]   INT          NOT NULL,
	[Horsepower]       INT          NOT NULL,
	[Weight]           INT          NOT NULL,
	[Speed]            SMALLINT     NOT NULL,
	[Acceleration]     SMALLINT     NOT NULL,
	[Braking]          SMALLINT     NOT NULL,
	[Handling]         SMALLINT     NOT NULL,
	[EnergyEfficiency] SMALLINT     NOT NULL,
	[Reliability]      SMALLINT     NOT NULL,
	[Durability]       SMALLINT     NOT NULL,
	[Offroad]          SMALLINT     NOT NULL,


	CONSTRAINT pk_LandVehicleFinish PRIMARY KEY(Id)
)
GO