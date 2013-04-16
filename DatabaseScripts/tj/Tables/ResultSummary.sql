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
    [SpoiledManualBallots] INT              NULL,
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
EXECUTE sp_addextendedproperty @name = N'MS_Description', @value = 'How many could have voted?', @level0type = N'SCHEMA', @level0name = N'tj', @level1type = N'TABLE', @level1name = N'ResultSummary', @level2type = N'COLUMN', @level2name = N'NumEligibleToVote';


GO
EXECUTE sp_addextendedproperty @name = N'MS_Description', @value = 'How many voted?', @level0type = N'SCHEMA', @level0name = N'tj', @level1type = N'TABLE', @level1name = N'ResultSummary', @level2type = N'COLUMN', @level2name = N'NumVoters';


GO
EXECUTE sp_addextendedproperty @name = N'MS_Description', @value = 'Must choose to use Auto or Manual', @level0type = N'SCHEMA', @level0name = N'tj', @level1type = N'TABLE', @level1name = N'ResultSummary', @level2type = N'COLUMN', @level2name = N'UseOnReports';


GO
EXECUTE sp_addextendedproperty @name = N'MS_Description', @value = 'Auto, Manual', @level0type = N'SCHEMA', @level0name = N'tj', @level1type = N'TABLE', @level1name = N'ResultSummary', @level2type = N'COLUMN', @level2name = N'ResultType';


GO
EXECUTE sp_addextendedproperty @name = N'MS_Description', @value = '', @level0type = N'SCHEMA', @level0name = N'tj', @level1type = N'TABLE', @level1name = N'ResultSummary', @level2type = N'COLUMN', @level2name = N'ElectionGuid';

