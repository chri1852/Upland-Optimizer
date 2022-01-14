CREATE TABLE [UPL].[RegisteredUser]
(
	[Id]                       INT IDENTITY(1,1) NOT NULL,
	[DiscordUserId]            DECIMAL(20,0)             ,
	[DiscordUsername]          VARCHAR(200)              ,
	[UplandUsername]           VARCHAR(200)      NOT NULL,
	[RunCount]                 INT               NOT NULL,
	[Paid]                     BIT               NOT NULL,
	[PropertyId]               BIGINT                    ,
	[Price]                    INT                       ,
	[SendUPX]                  INT               NOT NULL,
	[PasswordSalt]             VARCHAR(64)               ,
	[PasswordHash]             VARCHAR(64)               ,
	[DiscordVerified]          BIT               NOT NULL,
	[WebVerified]              BIT               NOT NULL,
	[VerifyType]               VARCHAR(3)                ,
	[VerifyExpirationDateTime] DATETIME

	CONSTRAINT pk_RegisteredUserId PRIMARY KEY(Id)
)
GO