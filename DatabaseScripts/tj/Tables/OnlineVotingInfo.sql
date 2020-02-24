CREATE TABLE [dbo].[OnlineVotingInfo] (
    [_RowId]               INT              IDENTITY (1, 1) NOT NULL,
    [ElectionGuid]         UNIQUEIDENTIFIER NOT NULL,
    [PersonGuid]           UNIQUEIDENTIFIER NOT NULL,
    [Email]                NVARCHAR (250)   NOT NULL,
    [WhenBallotCreated]    DATETIME2 (0)    NULL,
    [Status]               VARCHAR (10)     NOT NULL,
    [WhenStatus]           DATETIME2 (0)    NULL,
    [ListPool]             NVARCHAR (MAX)   NULL,
    [PoolLocked]           BIT              NULL,
    [HistoryStatus]        VARCHAR (MAX)    NULL,
    [NotifiedAboutOpening] BIT              NULL,
    CONSTRAINT [PK_OnlineVotingInfo] PRIMARY KEY CLUSTERED ([_RowId] ASC)
);





GO

CREATE UNIQUE NONCLUSTERED INDEX [IX_OnlineVotingInfo_Election]
    ON [dbo].[OnlineVotingInfo]([ElectionGuid] ASC, [Email] ASC)
    INCLUDE([PersonGuid], [WhenBallotCreated], [Status], [WhenStatus], [ListPool], [PoolLocked], [HistoryStatus]);


GO


GRANT SELECT, update, insert, delete
    ON OBJECT::[OnlineVotingInfo] TO [TallyJSite]
    AS [dbo];
Go
CREATE NONCLUSTERED INDEX [IX_OnlineVotingInfo_Person]
    ON [dbo].[OnlineVotingInfo]([PersonGuid] ASC)
    INCLUDE([Status], [WhenStatus], [PoolLocked]);


GO
CREATE NONCLUSTERED INDEX [IX_OnlineVotingInfo_ElectionPerson]
    ON [dbo].[OnlineVotingInfo]([ElectionGuid] ASC, [PersonGuid] ASC)
    INCLUDE([Email], [WhenBallotCreated], [Status], [WhenStatus], [ListPool], [PoolLocked], [HistoryStatus]);

