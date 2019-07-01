CREATE TABLE [dbo].[OnlineElection]
(
    [_RowId]            INT              IDENTITY (1, 1) NOT NULL,
    [ElectionGuid]      UNIQUEIDENTIFIER NOT NULL,
    [WhenOpen]          DATETIME2 (0)    NULL,
    [WhenClose]         DATETIME2 (0)    NULL,
    [CloseIsEstimate]   Bit NOT NULL default 1,
    [AllowResultView]   Bit NOT NULL default 1, 
    [HistoryWhen] VARCHAR(MAX) NULL, 
    CONSTRAINT [PK_OnlineElection] PRIMARY KEY (_RowId),

)

GO

CREATE UNIQUE INDEX [IX_OnlineElection_Election] ON [dbo].[OnlineElection] (ElectionGuid)
GO

CREATE UNIQUE INDEX [IX_OnlineElection_Dates] ON [dbo].[OnlineElection] (WhenOpen, WhenClose) include (ElectionGuid)
