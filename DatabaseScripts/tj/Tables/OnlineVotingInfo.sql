CREATE TABLE [dbo].[OnlineVotingInfo]
(
    [_RowId]            INT              IDENTITY (1, 1) NOT NULL,
    [ElectionGuid]      UNIQUEIDENTIFIER NOT NULL, -- source may be deleted
    [PersonGuid]        UNIQUEIDENTIFIER NOT NULL, -- source may be deleted
    [Email]             NVARCHAR (250)   NOT NULL,
    [WhenBallotCreated] DATETIME2 (0)    NULL, -- round to nearest minute?
    [Status]            VARCHAR (10)     NOT NULL,
    [WhenStatus]        DATETIME2 (0)    NULL,
    [ListPool]          varchar(max)     NULL,
    [PoolLocked]         BIT     NULL,
    [HistoryStatus]     VARCHAR(MAX) NULL, 
    CONSTRAINT [PK_OnlineVotingInfo] PRIMARY KEY (_RowId),

)

GO

CREATE UNIQUE INDEX [IX_OnlineVotingInfo_Election] ON [dbo].[OnlineVotingInfo] (ElectionGuid, EMail)
GO

