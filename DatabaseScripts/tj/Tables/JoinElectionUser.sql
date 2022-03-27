﻿CREATE TABLE [tj].[JoinElectionUser] (
    [_RowId]       INT              IDENTITY (1, 1) NOT NULL,
    [ElectionGuid] UNIQUEIDENTIFIER NOT NULL,
    [UserId]       UNIQUEIDENTIFIER NOT NULL,
    [Role]         VARCHAR (10)     NULL,
    [InviteEmail] NVARCHAR(150) NULL, 
    [InviteWhen] DATETIME2(0) NULL, 
    CONSTRAINT [PK_JoinElectionUser] PRIMARY KEY CLUSTERED ([_RowId] ASC),
    CONSTRAINT [FK_JoinElectionUser_Election] FOREIGN KEY ([ElectionGuid]) REFERENCES [tj].[Election] ([ElectionGuid]) ON DELETE CASCADE,
    CONSTRAINT [FK_JoinElectionUser_Users] FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users] ([UserId])
);


GO
GRANT UPDATE
    ON OBJECT::[tj].[JoinElectionUser] TO [TallyJSite]
    AS [dbo];


GO
GRANT SELECT
    ON OBJECT::[tj].[JoinElectionUser] TO [TallyJSite]
    AS [dbo];


GO
GRANT INSERT
    ON OBJECT::[tj].[JoinElectionUser] TO [TallyJSite]
    AS [dbo];


GO
GRANT DELETE
    ON OBJECT::[tj].[JoinElectionUser] TO [TallyJSite]
    AS [dbo];




GO

CREATE INDEX [IX_JoinElectionUser_UserId] ON [tj].[JoinElectionUser] (UserId)
Go

CREATE INDEX [IX_JoinElectionUser_ElectionGuid] ON [tj].[JoinElectionUser] (ElectionGuid)
