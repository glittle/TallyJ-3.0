CREATE TABLE [tj].[ResultSummary] (
    [_RowId]               INT              IDENTITY (1, 1) NOT NULL,
    [ElectionGuid]         UNIQUEIDENTIFIER NOT NULL,
    [ResultType]           CHAR (1)         NOT NULL,
    [UseOnReports]         BIT              NULL,
    [NumVoters]            INT              NULL,
    [NumEligibleToVote]    INT              NULL,
    [MailedInBallots]      INT              NULL,
    [DroppedOffBallots]    INT              NULL,
    [InPersonBallots]      INT              NULL,
    [SpoiledBallots]       INT              NULL,
    [SpoiledVotes]         INT              NULL,
    [TotalVotes]           INT              NULL,
    [BallotsReceived]      INT              NULL,
    [BallotsNeedingReview] INT              NULL,
    [CalledInBallots]      INT              NULL,
    [OnlineBallots]        INT              NULL,
    [SpoiledManualBallots] INT              NULL,
    [Custom1Ballots] INT NULL, 
    [Custom2Ballots] INT NULL, 
    [Custom3Ballots] INT NULL, 
    CONSTRAINT [PK_ResultSummary] PRIMARY KEY CLUSTERED ([_RowId] ASC),
    CONSTRAINT [FK_ResultSummary_Election1] FOREIGN KEY ([ElectionGuid]) REFERENCES [tj].[Election] ([ElectionGuid]) ON DELETE CASCADE
);






GO
GRANT UPDATE
    ON OBJECT::[tj].[ResultSummary] TO [TallyJSite]
    AS [dbo];


GO
GRANT SELECT
    ON OBJECT::[tj].[ResultSummary] TO [TallyJSite]
    AS [dbo];


GO
GRANT INSERT
    ON OBJECT::[tj].[ResultSummary] TO [TallyJSite]
    AS [dbo];


GO
GRANT DELETE
    ON OBJECT::[tj].[ResultSummary] TO [TallyJSite]
    AS [dbo];




GO
CREATE NONCLUSTERED INDEX [Ix_ResultSummary_Election]
    ON [tj].[ResultSummary]([ElectionGuid] ASC)
    INCLUDE([_RowId], [ResultType], [UseOnReports], [NumVoters], [NumEligibleToVote], [MailedInBallots], [DroppedOffBallots], [InPersonBallots], [SpoiledBallots], [SpoiledVotes], [TotalVotes], [BallotsReceived], [BallotsNeedingReview], [CalledInBallots], [OnlineBallots], [SpoiledManualBallots], Custom1Ballots, Custom2Ballots, Custom3Ballots);

