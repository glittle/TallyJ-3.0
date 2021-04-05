CREATE TABLE [tj].[Election] (
    [_RowId]                 INT              IDENTITY (1, 1) NOT NULL,
    [ElectionGuid]           UNIQUEIDENTIFIER CONSTRAINT [DF_Election_ElectionGuid] DEFAULT (CONVERT([uniqueidentifier],CONVERT([binary](10),newid(),(0))+CONVERT([binary](6),getdate(),(0)),(0))) NOT NULL,
    [Name]                   NVARCHAR (150)   NOT NULL,
    [Convenor]               NVARCHAR (150)   NULL,
    [DateOfElection]         DATETIME2 (0)    NULL,
    [ElectionType]           VARCHAR (5)      NULL,
    [ElectionMode]           VARCHAR (1)      NULL,
    [NumberToElect]          INT              NULL,
    [NumberExtra]            INT              NULL,
    [CanVote]                VARCHAR (1)      NULL,
    [CanReceive]             VARCHAR (1)      NULL,
    [LastEnvNum]             INT              NULL,
    [TallyStatus]            VARCHAR (15)     NULL,
    [ShowFullReport]         BIT              NULL,
    [LinkedElectionGuid]     UNIQUEIDENTIFIER NULL,
    [LinkedElectionKind]     VARCHAR (2)      NULL,
    [OwnerLoginId]           VARCHAR (50)     NULL,
    [ElectionPasscode]       NVARCHAR (50)    NULL,
    [ListedForPublicAsOf]    DATETIME2 (7)    NULL,
    [_RowVersion]            ROWVERSION       NOT NULL,
    [ListForPublic]          BIT              NULL,
    [ShowAsTest]             BIT              NULL,
    [UseCallInButton]        BIT              NULL,
    [HidePreBallotPages]     BIT              NULL,
    [MaskVotingMethod]       BIT              NULL,
    [OnlineWhenOpen]         DATETIME2 (0)    NULL,
    [OnlineWhenClose]        DATETIME2 (0)    NULL,
    [OnlineCloseIsEstimate]  BIT              DEFAULT ((1)) NOT NULL,
    [OnlineSelectionProcess] VARCHAR (1)      NULL,
    [OnlineAnnounced]        DATETIME2 (0)    NULL,
    [EmailFromAddress]       NVARCHAR (250)   NULL,
    [EmailFromName]          NVARCHAR (100)   NULL,
    [EmailText]              NVARCHAR (MAX)   NULL,
    [SmsText]                NVARCHAR (500)   NULL,
    [EmailSubject]           NVARCHAR (250)   NULL,
    [CustomMethods] NVARCHAR(50) NULL, 
    [VotingMethods] VARCHAR(10) NULL, 
    CONSTRAINT [PK_Election] PRIMARY KEY CLUSTERED ([_RowId] ASC)
);












GO
CREATE UNIQUE NONCLUSTERED INDEX [IX_Election]
    ON [tj].[Election]([ElectionGuid] ASC)
    INCLUDE([Name], [Convenor], [DateOfElection], [ElectionType], [ElectionMode], [NumberToElect], [NumberExtra], [CanVote], [CanReceive], [LastEnvNum], [TallyStatus], [ShowFullReport], [LinkedElectionGuid], [LinkedElectionKind], [OwnerLoginId], [ElectionPasscode], [ListedForPublicAsOf], [_RowVersion], [ListForPublic], [ShowAsTest], [UseCallInButton], [HidePreBallotPages], [MaskVotingMethod], [OnlineWhenOpen], [OnlineWhenClose], [OnlineCloseIsEstimate], [OnlineSelectionProcess], [OnlineAnnounced]);




GO

GRANT UPDATE
    ON OBJECT::[tj].[Election] TO [TallyJSite]
    AS [dbo];


GO
GRANT SELECT
    ON OBJECT::[tj].[Election] TO [TallyJSite]
    AS [dbo];


GO
GRANT INSERT
    ON OBJECT::[tj].[Election] TO [TallyJSite]
    AS [dbo];


GO
GRANT DELETE
    ON OBJECT::[tj].[Election] TO [TallyJSite]
    AS [dbo];



