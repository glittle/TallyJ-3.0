CREATE TABLE [dbo].[OnlineVoter] (
    [_RowId]         INT            IDENTITY (1, 1) NOT NULL,
    [VoterId]        NVARCHAR (250) NOT NULL,
    [VoterIdType]    VARCHAR (1)    CONSTRAINT [DF_OnlineVoter_VoterIdType] DEFAULT ('E') NOT NULL,
    [WhenRegistered] DATETIME2 (0)  NULL,
    [WhenLastLogin]  DATETIME2 (0)  NULL,
    [EmailCodes]     VARCHAR (25)   NULL,
    [Country]        NVARCHAR (50)  NULL,
    [OtherInfo]      NVARCHAR (200) NULL,
    CONSTRAINT [PK_OnlineVoter] PRIMARY KEY CLUSTERED ([_RowId] ASC)
);





GO




GO




GO

GO
GRANT SELECT, update, insert, delete
    ON OBJECT::[OnlineVoter] TO [TallyJSite]
    AS [dbo];
Go
CREATE UNIQUE NONCLUSTERED INDEX [IX_OnlineVoter_Id]
    ON [dbo].[OnlineVoter]([VoterId] ASC)
    INCLUDE([VoterIdType], [WhenRegistered], [WhenLastLogin], [EmailCodes], [Country], [OtherInfo]);

