-- Version 3.3.0 
-- 21 February 2022 / 17 Dominion 178

ALTER TABLE [dbo].[OnlineVoter]
    ADD [VerifyCode]          VARCHAR (15)  NULL,
        [VerifyCodeDate]      DATETIME2 (0) NULL,
        [VerifyAttempts]      INT           NULL,
        [VerifyAttemptsStart] DATETIME2 (0) NULL;

