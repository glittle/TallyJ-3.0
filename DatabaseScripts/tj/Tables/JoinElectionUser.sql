CREATE TABLE [tj].[JoinElectionUser] (
    [_RowId]       INT              IDENTITY (1, 1) NOT NULL,
    [ElectionGuid] UNIQUEIDENTIFIER NOT NULL,
    [UserId]       UNIQUEIDENTIFIER NOT NULL,
    [Role]         VARCHAR (10)     NULL,
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
EXECUTE sp_addextendedproperty @name = N'MS_Description', @value = 'No cascade. Must ensure at least one exists', @level0type = N'SCHEMA', @level0name = N'tj', @level1type = N'TABLE', @level1name = N'JoinElectionUser', @level2type = N'CONSTRAINT', @level2name = N'FK_JoinElectionUser_Election';

