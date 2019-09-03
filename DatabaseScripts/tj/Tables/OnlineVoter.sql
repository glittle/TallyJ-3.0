CREATE TABLE [dbo].[OnlineVoter]
(
    [_RowId]            INT              IDENTITY (1, 1) NOT NULL,
    [Email]             NVARCHAR (250)   NOT NULL,
    [WhenRegistered]    DATETIME2 (0)    NULL,
    [WhenLastLogin]     DATETIME2 (0)    NULL,
    EmailCodes          Varchar(10)      NULL,
    CONSTRAINT [PK_OnlineVoter] PRIMARY KEY (_RowId),
)

GO

CREATE UNIQUE INDEX [IX_OnlineElection_Email] ON [dbo].[OnlineVoter] (Email)
GO

GO
GRANT SELECT, update, insert, delete
    ON OBJECT::[OnlineVoter] TO [TallyJSite]
    AS [dbo];
Go
