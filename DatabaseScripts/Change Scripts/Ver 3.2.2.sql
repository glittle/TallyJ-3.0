-- Verison 3.2.2

ALTER TABLE [tj].[Election]
    ADD [CustomMethods] NVARCHAR (50) NULL,
        [VotingMethods] VARCHAR (10)  NULL;

-- default VotingMethods on existing elections to "PDM":
-- update tj.Election set VotingMethods = 'PDM' + case when UseCallInButton = 1 then 'C' else '' end where VotingMethods is null

GO
ALTER TABLE [tj].[ResultSummary]
    ADD [Custom1Ballots] INT NULL,
        [Custom2Ballots] INT NULL,
        [Custom3Ballots] INT NULL;


-- add custom1,2,3 to the included columns in [Ix_ResultSummary_Election]
DROP INDEX [Ix_ResultSummary_Election]
    ON [tj].[ResultSummary];
GO
CREATE NONCLUSTERED INDEX [Ix_ResultSummary_Election]
    ON [tj].[ResultSummary]([ElectionGuid] ASC)
    INCLUDE([_RowId], [ResultType], [UseOnReports], [NumVoters], [NumEligibleToVote], [MailedInBallots], [DroppedOffBallots], [InPersonBallots], [SpoiledBallots], [SpoiledVotes], [TotalVotes], [BallotsReceived], [BallotsNeedingReview], [CalledInBallots], [OnlineBallots], [SpoiledManualBallots], [Custom1Ballots], [Custom2Ballots], [Custom3Ballots]);

GO