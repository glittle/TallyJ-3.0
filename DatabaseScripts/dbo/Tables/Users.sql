﻿CREATE TABLE [dbo].[Users] (
    [ApplicationId]    UNIQUEIDENTIFIER NOT NULL,
    [UserId]           UNIQUEIDENTIFIER NOT NULL,
    [UserName]         NVARCHAR (50)    NOT NULL,
    [IsAnonymous]      BIT              NOT NULL,
    [LastActivityDate] DATETIME         NOT NULL,
    PRIMARY KEY CLUSTERED ([UserId] ASC),
    CONSTRAINT [UserApplication] FOREIGN KEY ([ApplicationId]) REFERENCES [dbo].[Applications] ([ApplicationId])
);






GO
GRANT UPDATE
    ON OBJECT::[dbo].[Users] TO [TallyJSite]
    AS [dbo];


GO
GRANT SELECT
    ON OBJECT::[dbo].[Users] TO [TallyJSite]
    AS [dbo];


GO
GRANT INSERT
    ON OBJECT::[dbo].[Users] TO [TallyJSite]
    AS [dbo];


GO
GRANT DELETE
    ON OBJECT::[dbo].[Users] TO [TallyJSite]
    AS [dbo];


GO
CREATE UNIQUE NONCLUSTERED INDEX [IDX_UserName]
    ON [dbo].[Users]([UserName] ASC);


GO
CREATE NONCLUSTERED INDEX [IX_Users_UserName]
    ON [dbo].[Users]([UserName] ASC)
    INCLUDE([ApplicationId], [UserId], [IsAnonymous], [LastActivityDate]);

