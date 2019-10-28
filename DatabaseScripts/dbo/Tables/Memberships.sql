CREATE TABLE [dbo].[Memberships] (
    [ApplicationId]                           UNIQUEIDENTIFIER NOT NULL,
    [UserId]                                  UNIQUEIDENTIFIER NOT NULL,
    [Password]                                NVARCHAR (128)   NOT NULL,
    [PasswordFormat]                          INT              NOT NULL,
    [PasswordSalt]                            NVARCHAR (128)   NOT NULL,
    [Email]                                   NVARCHAR (256)   NULL,
    [PasswordQuestion]                        NVARCHAR (256)   NULL,
    [PasswordAnswer]                          NVARCHAR (128)   NULL,
    [IsApproved]                              BIT              NOT NULL,
    [IsLockedOut]                             BIT              NOT NULL,
    [CreateDate]                              DATETIME         NOT NULL,
    [LastLoginDate]                           DATETIME         NOT NULL,
    [LastPasswordChangedDate]                 DATETIME         NOT NULL,
    [LastLockoutDate]                         DATETIME         NOT NULL,
    [FailedPasswordAttemptCount]              INT              NOT NULL,
    [FailedPasswordAttemptWindowStart]        DATETIME         NOT NULL,
    [FailedPasswordAnswerAttemptCount]        INT              NOT NULL,
    [FailedPasswordAnswerAttemptWindowsStart] DATETIME         NOT NULL,
    [Comment]                                 NVARCHAR (256)   NULL,
    PRIMARY KEY CLUSTERED ([UserId] ASC),
    CONSTRAINT [MembershipApplication] FOREIGN KEY ([ApplicationId]) REFERENCES [dbo].[Applications] ([ApplicationId]),
    CONSTRAINT [MembershipUser] FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users] ([UserId])
);




GO
GRANT UPDATE
    ON OBJECT::[dbo].[Memberships] TO [TallyJSite]
    AS [dbo];


GO
GRANT SELECT
    ON OBJECT::[dbo].[Memberships] TO [TallyJSite]
    AS [dbo];


GO
GRANT INSERT
    ON OBJECT::[dbo].[Memberships] TO [TallyJSite]
    AS [dbo];


GO
GRANT DELETE
    ON OBJECT::[dbo].[Memberships] TO [TallyJSite]
    AS [dbo];


GO
CREATE NONCLUSTERED INDEX [IX_Memberships]
    ON [dbo].[Memberships]([UserId] ASC)
    INCLUDE([ApplicationId], [Password], [PasswordFormat], [PasswordSalt], [Email], [PasswordQuestion], [PasswordAnswer], [IsApproved], [IsLockedOut], [CreateDate], [LastLoginDate], [LastPasswordChangedDate], [LastLockoutDate], [FailedPasswordAttemptCount], [FailedPasswordAttemptWindowStart], [FailedPasswordAnswerAttemptCount], [FailedPasswordAnswerAttemptWindowsStart], [Comment]);

