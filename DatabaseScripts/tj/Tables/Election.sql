CREATE TABLE [tj].[Election] (
    [_RowId]              INT              IDENTITY (1, 1) NOT NULL,
    [ElectionGuid]        UNIQUEIDENTIFIER CONSTRAINT [DF_Election_ElectionGuid] DEFAULT (CONVERT([uniqueidentifier],CONVERT([binary](10),newid(),0)+CONVERT([binary](6),getdate(),0),0)) NOT NULL,
    [Name]                NVARCHAR (150)   NOT NULL,
    [Convenor]            NVARCHAR (150)   NULL,
    [DateOfElection]      DATETIME2 (0)    NULL,
    [ElectionType]        VARCHAR (5)      NULL,
    [ElectionMode]        VARCHAR (1)      NULL,
    [NumberToElect]       INT              NULL,
    [NumberExtra]         INT              NULL,
    [CanVote]             VARCHAR (1)      NULL,
    [CanReceive]          VARCHAR (1)      NULL,
    [LastEnvNum]          INT              NULL,
    [TallyStatus]         VARCHAR (15)     NULL,
    [ShowFullReport]      BIT              NULL,
    [LinkedElectionGuid]  UNIQUEIDENTIFIER NULL,
    [LinkedElectionKind]  VARCHAR (2)      NULL,
    [OwnerLoginId]        VARCHAR (50)     NULL,
    [ElectionPasscode]    NVARCHAR (50)    NULL,
    [ListedForPublicAsOf] DATETIME2 (7)    NULL,
    [_RowVersion]         ROWVERSION       NOT NULL,
    [ListForPublic]       BIT              NULL,
    [ShowAsTest]          BIT              NULL,
    [UseCallInButton]     BIT              NULL,
    [HidePreBallotPages]  BIT              NULL,
    [MaskVotingMethod]    BIT              NULL,
    [OnlineWhenOpen]          DATETIME2 (0)    NULL,
    [OnlineWhenClose]         DATETIME2 (0)    NULL,
    [OnlineCloseIsEstimate]   Bit NOT NULL default 1,
    [OnlineAllowResultView]   Bit NOT NULL default 1, 
    CONSTRAINT [PK_Election] PRIMARY KEY CLUSTERED ([_RowId] ASC)
);






GO
CREATE UNIQUE NONCLUSTERED INDEX [IX_Election]
    ON [tj].[Election]([ElectionGuid] ASC);


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



