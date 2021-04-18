-- Verison 3.2.10

GO
ALTER TABLE [tj].[ResultSummary]
    ADD [ImportedBallots] INT NULL;


-- add to the included columns in [Ix_ResultSummary_Election]
DROP INDEX [Ix_ResultSummary_Election]
    ON [tj].[ResultSummary];
GO
CREATE NONCLUSTERED INDEX [Ix_ResultSummary_Election]
    ON [tj].[ResultSummary]([ElectionGuid] ASC)
    INCLUDE([_RowId], [ResultType], [UseOnReports], [NumVoters], [NumEligibleToVote], [MailedInBallots], 
    [DroppedOffBallots], [InPersonBallots], [SpoiledBallots], [SpoiledVotes], [TotalVotes], 
    [BallotsReceived], [BallotsNeedingReview], [CalledInBallots], [OnlineBallots], [SpoiledManualBallots], 
    [Custom1Ballots], [Custom2Ballots], [Custom3Ballots], [ImportedBallots]);

GO