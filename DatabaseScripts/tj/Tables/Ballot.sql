CREATE TABLE [tj].[Ballot] (
    [_RowId]              INT              IDENTITY (1, 1) NOT NULL,
    [LocationGuid]        UNIQUEIDENTIFIER NOT NULL,
    [BallotGuid]          UNIQUEIDENTIFIER CONSTRAINT [DF_Ballot_BallotGuid] DEFAULT (CONVERT([uniqueidentifier],CONVERT([binary](10),newid(),0)+CONVERT([binary](6),getdate(),0),0)) NOT NULL,
    [StatusCode]          VARCHAR (10)     NOT NULL,
    [ComputerCode]        VARCHAR (2)      NOT NULL,
    [BallotNumAtComputer] INT              NOT NULL,
    [_BallotCode]         AS               ([ComputerCode]+CONVERT([varchar],[BallotNumAtComputer],(0))) PERSISTED,
    [TellerAtKeyboard]    UNIQUEIDENTIFIER NULL,
    [TellerAssisting]     UNIQUEIDENTIFIER NULL,
    [_RowVersion]         ROWVERSION       NOT NULL,
    CONSTRAINT [PK_Ballot] PRIMARY KEY CLUSTERED ([_RowId] ASC),
    CONSTRAINT [FK_Ballot_Location1] FOREIGN KEY ([LocationGuid]) REFERENCES [tj].[Location] ([LocationGuid]) ON DELETE CASCADE,
    CONSTRAINT [FK_Ballot_Teller] FOREIGN KEY ([TellerAssisting]) REFERENCES [tj].[Teller] ([TellerGuid]),
    CONSTRAINT [FK_Ballot_Teller1] FOREIGN KEY ([TellerAtKeyboard]) REFERENCES [tj].[Teller] ([TellerGuid])
);






GO
CREATE UNIQUE NONCLUSTERED INDEX [IX_Ballot]
    ON [tj].[Ballot]([BallotGuid] ASC);


GO

CREATE
/*

Purpose: Remove ResultSummary when a Ballot is touched


*/
TRIGGER [tj].[Ballot_ClearResultSummary] ON [tj].[Ballot]
FOR INSERT, UPDATE, DELETE
AS

   delete from rs
   from ResultSummary rs
     join Location l on l.ElectionGuid = rs.ElectionGuid
	 join (select LocationGuid from inserted union select LocationGuid from deleted) id on id.LocationGuid = l.LocationGuid
   where rs.ResultType != 'M'
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

