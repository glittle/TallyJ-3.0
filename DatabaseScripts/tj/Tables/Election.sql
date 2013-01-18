CREATE TABLE [tj].[Election] (
    [_RowId]              INT              IDENTITY (1, 1) NOT NULL,
    [ElectionGuid]        UNIQUEIDENTIFIER CONSTRAINT [DF_Election_ElectionGuid] DEFAULT (newsequentialid()) NOT NULL,
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
    CONSTRAINT [PK_Election] PRIMARY KEY CLUSTERED ([_RowId] ASC)
);


GO
CREATE UNIQUE NONCLUSTERED INDEX [IX_Election]
    ON [tj].[Election]([ElectionGuid] ASC);


GO
CREATE
/*

Purpose: Remove ResultSummary when a Ballot is touched


*/
TRIGGER tj.Election_ClearResultSummary ON tj.Election
FOR UPDATE
AS
   if( 
        UPDATE(ElectionType) or
        UPDATE(ElectionMode) or
        UPDATE(NumberToElect) or
        UPDATE(NumberExtra)
		)
   begin
	   delete from rs
	   from ResultSummary rs
		 join inserted id on id.ElectionGuid = rs.ElectionGuid
   end
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


GO
EXECUTE sp_addextendedproperty @name = N'MS_Description', @value = 'For tie-breaks, etc.', @level0type = N'SCHEMA', @level0name = N'tj', @level1type = N'TABLE', @level1name = N'Election', @level2type = N'COLUMN', @level2name = N'LinkedElectionKind';

