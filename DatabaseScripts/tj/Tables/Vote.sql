CREATE TABLE [tj].[Vote] (
    [_RowId]                  INT              IDENTITY (1, 1) NOT NULL,
    [BallotGuid]              UNIQUEIDENTIFIER NOT NULL,
    [PositionOnBallot]        INT              NOT NULL,
    [PersonGuid]              UNIQUEIDENTIFIER NULL,
    [StatusCode]              VARCHAR (10)     NOT NULL,
    [InvalidReasonGuid]       UNIQUEIDENTIFIER NULL,
    [SingleNameElectionCount] INT              NULL,
    [_RowVersion]             ROWVERSION       NOT NULL,
    [PersonCombinedInfo]      NVARCHAR (MAX)   NULL,
    [OnlineVoteRaw]           NVARCHAR (MAX)   NULL,
    CONSTRAINT [PK_Vote] PRIMARY KEY CLUSTERED ([_RowId] ASC),
    CONSTRAINT [FK_Vote_Ballot] FOREIGN KEY ([BallotGuid]) REFERENCES [tj].[Ballot] ([BallotGuid]) ON DELETE CASCADE,
    CONSTRAINT [FK_Vote_Person1] FOREIGN KEY ([PersonGuid]) REFERENCES [tj].[Person] ([PersonGuid])
);










GO

GRANT UPDATE
    ON OBJECT::[tj].[Vote] TO [TallyJSite]
    AS [dbo];


GO
GRANT SELECT
    ON OBJECT::[tj].[Vote] TO [TallyJSite]
    AS [dbo];


GO
GRANT INSERT
    ON OBJECT::[tj].[Vote] TO [TallyJSite]
    AS [dbo];


GO
GRANT DELETE
    ON OBJECT::[tj].[Vote] TO [TallyJSite]
    AS [dbo];


GO
CREATE NONCLUSTERED INDEX [IX_VotePerson]
    ON [tj].[Vote]([PersonGuid] ASC);


GO
CREATE NONCLUSTERED INDEX [IX_VoteBallot]
    ON [tj].[Vote]([BallotGuid] ASC, [PositionOnBallot] ASC)
    INCLUDE([_RowId], [PersonGuid], [StatusCode], [InvalidReasonGuid], [SingleNameElectionCount], [PersonCombinedInfo], [OnlineVoteRaw], [_RowVersion]);





