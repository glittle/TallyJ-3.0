ALTER TABLE [tj].[JoinElectionUser]
    ADD [InviteEmail] NVARCHAR (150) NULL,
        [InviteWhen]  DATETIME2 (0)  NULL;

GO
CREATE NONCLUSTERED INDEX [IX_JoinElectionUser_ElectionGuid]
    ON [tj].[JoinElectionUser]([ElectionGuid] ASC);


GO
CREATE NONCLUSTERED INDEX [IX_JoinElectionUser_UserId]
    ON [tj].[JoinElectionUser]([UserId] ASC);

GO

/*

A single 'fake' user must be added to the Users table in the database

*/
insert into Users (ApplicationId, UserId, UserName, IsAnonymous, LastActivityDate)
  select top 1 ApplicationId, '00000000-0000-0000-0000-000000000000', 'PENDING', 1, sysdatetime()
  from Applications
