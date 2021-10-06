CREATE TABLE [UPL].[RegisteredUser]
(
	[Id]                INT IDENTITY(1,1) NOT NULL,
	[DiscordUserId]     DECIMAL(20,0)     NOT NULL,
	[DiscordUsername]   VARCHAR(200)      NOT NULL,
	[UplandUsername]    VARCHAR(200)              ,
	[RunCount]          INT               NOT NULL,
	[Paid]              BIT               NOT NULL,
	[PropertyId]        BIGINT            NOT NULL,
	[Price]             INT               NOT NULL,
	[Verified]          BIT               NOT NULL,

	CONSTRAINT pk_RegisteredUserId PRIMARY KEY(Id)
)
GO