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
    CONSTRAINT [PK_Vote] PRIMARY KEY CLUSTERED ([_RowId] ASC),
    CONSTRAINT [FK_Vote_Ballot] FOREIGN KEY ([BallotGuid]) REFERENCES [tj].[Ballot] ([BallotGuid]),
    CONSTRAINT [FK_Vote_Person1] FOREIGN KEY ([PersonGuid]) REFERENCES [tj].[Person] ([PersonGuid])
);




GO

CREATE
/*

Purpose: Remove ResultSummary when a Vote is touched


*/
TRIGGER [tj].[Vote_ClearResultSummary] ON [tj].[Vote]
FOR INSERT, UPDATE, DELETE
AS

   delete from rs
   from ResultSummary rs
     join Location l on l.ElectionGuid = rs.ElectionGuid
	 join Ballot b on b.LocationGuid = l.LocationGuid
	 join (select BallotGuid from inserted union select BallotGuid from deleted) id on id.BallotGuid = b.BallotGuid
   where rs.ResultType != 'M'
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
    ON [tj].[Vote]([BallotGuid] ASC)
    INCLUDE([_RowId], [PositionOnBallot], [PersonGuid], [StatusCode], [InvalidReasonGuid], [SingleNameElectionCount], [PersonCombinedInfo]);

