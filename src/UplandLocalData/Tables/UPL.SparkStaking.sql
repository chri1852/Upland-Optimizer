﻿CREATE TABLE [UPL].[SparkStaking] 
(
	[Id]                 INT IDENTITY(1,1) NOT NULL,
	[DGoodId]            INT               NOT NULL, 
	[EOSUserId]			 INT               NOT NULL,
	[Amount]             DECIMAL(6,2)      NOT NULL,
	[Start]              DATETIME	       NOT NULL,
	[End]                DATETIME


	CONSTRAINT pk_SparkStakingId PRIMARY KEY(Id)
)
GO