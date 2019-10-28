CREATE TABLE [tj].[Ballot] (
    [_RowId]              INT              IDENTITY (1, 1) NOT NULL,
    [LocationGuid]        UNIQUEIDENTIFIER NOT NULL,
    [BallotGuid]          UNIQUEIDENTIFIER NOT NULL,
    [StatusCode]          VARCHAR (10)     NOT NULL,
    [ComputerCode]        VARCHAR (2)      NOT NULL,
    [BallotNumAtComputer] INT              NOT NULL,
    [_BallotCode]         AS               ([ComputerCode]+CONVERT([varchar],[BallotNumAtComputer],(0))) PERSISTED,
    [Teller1]             NVARCHAR (25)    NULL,
    [Teller2]             NVARCHAR (25)    NULL,
    [_RowVersion]         ROWVERSION       NOT NULL,
    CONSTRAINT [PK_Ballot] PRIMARY KEY CLUSTERED ([_RowId] ASC),
    CONSTRAINT [FK_Ballot_Location1] FOREIGN KEY ([LocationGuid]) REFERENCES [tj].[Location] ([LocationGuid]) ON DELETE CASCADE
);




GO
CREATE UNIQUE NONCLUSTERED INDEX [IX_Ballot]
    ON [tj].[Ballot]([BallotGuid] ASC);

GO

GRANT UPDATE
    ON OBJECT::[tj].[Ballot] TO [TallyJSite]
    AS [dbo];


GO
GRANT SELECT
    ON OBJECT::[tj].[Ballot] TO [TallyJSite]
    AS [dbo];


GO
GRANT INSERT
    ON OBJECT::[tj].[Ballot] TO [TallyJSite]
    AS [dbo];


GO
GRANT DELETE
    ON OBJECT::[tj].[Ballot] TO [TallyJSite]
    AS [dbo];


GO
CREATE NONCLUSTERED INDEX [IX_Ballot_Location]
    ON [tj].[Ballot]([LocationGuid] ASC)
    INCLUDE([BallotGuid], [StatusCode], [ComputerCode], [BallotNumAtComputer], [_BallotCode], [Teller1], [Teller2], [_RowVersion]);


GO
CREATE NONCLUSTERED INDEX [IX_Ballot_Code]
    ON [tj].[Ballot]([ComputerCode] ASC)
    INCLUDE([BallotGuid], [StatusCode], [BallotNumAtComputer], [_BallotCode], [Teller1], [Teller2], [_RowVersion], [LocationGuid]);

